using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class PasskeyAuthentication(DiaperScoutDbContext db, IFido2 fido2, TimeProvider clock)
    : IPasskeyAuthentication
{
    public async Task<PasskeyOptions> BeginRegistrationAsync(AuthenticatedUser actor, string binding, CancellationToken cancellationToken)
    {
        await RequireActiveUserAsync(actor.UserId, cancellationToken);
        var email = await db.UserEmails.AsNoTracking().Where(x => x.UserId == actor.UserId)
            .Select(x => x.Email).FirstOrDefaultAsync(cancellationToken)
            ?? throw new PasskeyVerificationException();
        var name = await db.ExplorerProfiles.AsNoTracking().Where(x => x.UserId == actor.UserId)
            .Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken);
        var credentials = await db.PasskeyCredentials.AsNoTracking().Where(x => x.UserId == actor.UserId)
            .Select(x => x.CredentialId).ToListAsync(cancellationToken);
        if (credentials.Count >= 20) throw new PasskeyVerificationException();

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User { Id = actor.UserId.ToByteArray(), Name = email, DisplayName = string.IsNullOrWhiteSpace(name) ? email : name },
            ExcludeCredentials = credentials.Select(id => new PublicKeyCredentialDescriptor(id)).ToArray(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None
        });
        return await SaveChallengeAsync(actor.UserId, PasskeyCeremony.Registration, binding, options.ToJson(), cancellationToken);
    }

    public async Task<PasskeyItem> CompleteRegistrationAsync(AuthenticatedUser actor, CompletePasskeyRequest request, CancellationToken cancellationToken)
    {
        await RequireActiveUserAsync(actor.UserId, cancellationToken);
        var challenge = await ConsumeChallengeAsync(request, PasskeyCeremony.Registration, actor.UserId, cancellationToken);
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100) throw new PasskeyVerificationException();
        var response = Parse<AuthenticatorAttestationRawResponse>(request.Credential);
        if (response.RawId is not { Length: > 0 and <= 1024 } || response.Response?.ClientDataJson is null
            || response.Response.AttestationObject is null || response.ClientExtensionResults is null)
            throw new PasskeyVerificationException();
        ValidateClientData(response.Response.ClientDataJson);

        RegisteredPublicKeyCredential result;
        try
        {
            result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = response,
                OriginalOptions = CredentialCreateOptions.FromJson(challenge.OptionsJson),
                IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                    !await db.PasskeyCredentials.AnyAsync(x => x.CredentialId == args.CredentialId, ct)
            }, cancellationToken);
        }
        catch (Exception error) when (IsInvalidCredential(error)) { throw new PasskeyVerificationException(); }

        var credential = new PasskeyCredential(actor.UserId, result.Id, result.PublicKey, result.SignCount,
            result.IsBackupEligible, result.IsBackedUp, name, clock.GetUtcNow());
        db.PasskeyCredentials.Add(credential);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new PasskeyVerificationException(); }
        return ToItem(credential);
    }

    public Task<PasskeyOptions> BeginSignInAsync(string binding, CancellationToken cancellationToken)
    {
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            UserVerification = UserVerificationRequirement.Required
        });
        return SaveChallengeAsync(null, PasskeyCeremony.SignIn, binding, options.ToJson(), cancellationToken);
    }

    public async Task<PasswordlessAuthenticationResult> CompleteSignInAsync(CompletePasskeyRequest request, CancellationToken cancellationToken)
    {
        var challenge = await ConsumeChallengeAsync(request, PasskeyCeremony.SignIn, null, cancellationToken);
        var response = Parse<AuthenticatorAssertionRawResponse>(request.Credential);
        if (response.RawId is not { Length: > 0 and <= 1024 } || response.Response?.UserHandle is not { Length: 16 }
            || response.Response.AuthenticatorData is not { Length: >= 37 } || response.Response.Signature is null
            || response.Response.ClientDataJson is null || response.ClientExtensionResults is null)
            throw new PasskeyVerificationException();
        ValidateClientData(response.Response.ClientDataJson);
        var credential = await db.PasskeyCredentials.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CredentialId == response.RawId, cancellationToken)
            ?? throw new PasskeyVerificationException();
        var user = await RequireActiveUserAsync(credential.UserId, cancellationToken);
        if (!response.Response.UserHandle.AsSpan().SequenceEqual(user.Id.ToByteArray()))
            throw new PasskeyVerificationException();

        VerifyAssertionResult result;
        try
        {
            if (AuthenticatorAssertionResponse.Parse(response).AuthenticatorData.IsBackupEligible != credential.BackupEligible)
                throw new PasskeyVerificationException();
            result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = response,
                OriginalOptions = AssertionOptions.FromJson(challenge.OptionsJson),
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = checked((uint)credential.SignCount),
                IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(
                    args.CredentialId.AsSpan().SequenceEqual(credential.CredentialId)
                    && args.UserHandle.AsSpan().SequenceEqual(user.Id.ToByteArray()))
            }, cancellationToken);
        }
        catch (Exception error) when (IsInvalidCredential(error)) { throw new PasskeyVerificationException(); }

        // A concurrent removal or assertion must not be overwritten after verification.
        var now = clock.GetUtcNow();
        var version = Guid.NewGuid();
        var updated = await db.PasskeyCredentials.Where(x => x.Id == credential.Id && x.Version == credential.Version)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SignCount, (long)result.SignCount)
                .SetProperty(x => x.BackedUp, result.IsBackedUp).SetProperty(x => x.LastUsedAtUtc, now)
                .SetProperty(x => x.Version, version), cancellationToken);
        if (updated != 1) throw new PasskeyVerificationException();
        var roles = await db.PrivilegedRoleAssignments.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .Select(x => x.Role).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        return new PasswordlessAuthenticationResult(user.Id, user.Subject, roles);
    }

    public async Task<IReadOnlyList<PasskeyItem>> GetPasskeysAsync(AuthenticatedUser actor, CancellationToken cancellationToken)
    {
        await RequireActiveUserAsync(actor.UserId, cancellationToken);
        return await db.PasskeyCredentials.AsNoTracking().Where(x => x.UserId == actor.UserId)
            .OrderBy(x => x.CreatedAtUtc).Select(x => new PasskeyItem(x.Id, x.Name, x.CreatedAtUtc, x.LastUsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemovePasskeyAsync(AuthenticatedUser actor, Guid id, CancellationToken cancellationToken)
    {
        await RequireActiveUserAsync(actor.UserId, cancellationToken);
        return await db.PasskeyCredentials.Where(x => x.Id == id && x.UserId == actor.UserId)
            .ExecuteDeleteAsync(cancellationToken) == 1;
    }

    private async Task<User> RequireActiveUserAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Status == UserAccountStatus.Active, cancellationToken)
        ?? throw new PasskeyVerificationException();

    private async Task<PasskeyOptions> SaveChallengeAsync(Guid? userId, PasskeyCeremony ceremony, string binding,
        string json, CancellationToken cancellationToken)
    {
        var hash = BindingHash(binding);
        var now = clock.GetUtcNow();
        await db.PasskeyChallenges.Where(x => x.ExpiresAtUtc <= now).ExecuteDeleteAsync(cancellationToken);
        var challenge = new PasskeyChallenge(userId, ceremony, hash, json, now.AddMinutes(5));
        db.PasskeyChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        return new PasskeyOptions(challenge.Id, JsonSerializer.Deserialize<JsonElement>(json));
    }

    private async Task<PasskeyChallenge> ConsumeChallengeAsync(CompletePasskeyRequest request, PasskeyCeremony ceremony,
        Guid? userId, CancellationToken cancellationToken)
    {
        var hash = BindingHash(request.Binding);
        var now = clock.GetUtcNow();
        var query = db.PasskeyChallenges.Where(x => x.Id == request.RequestId && x.Ceremony == ceremony
            && x.UserId == userId && x.BindingHash == hash && x.UsedAtUtc == null && x.ExpiresAtUtc > now);
        if (await query.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, now), cancellationToken) != 1)
            throw new PasskeyVerificationException();
        return await db.PasskeyChallenges.AsNoTracking().SingleAsync(x => x.Id == request.RequestId, cancellationToken);
    }

    private static string BindingHash(string binding)
    {
        if (string.IsNullOrWhiteSpace(binding) || binding.Length is < 32 or > 128) throw new PasskeyVerificationException();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(binding)));
    }

    private static T Parse<T>(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object || json.GetRawText().Length > 32768) throw new PasskeyVerificationException();
        try { return json.Deserialize<T>() ?? throw new PasskeyVerificationException(); }
        catch (JsonException) { throw new PasskeyVerificationException(); }
    }

    private static bool IsInvalidCredential(Exception error) => error is Fido2VerificationException
        or ArgumentException or FormatException or InvalidOperationException or System.Formats.Cbor.CborContentException;

    private static void ValidateClientData(byte[] data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("topOrigin", out _)
                || (root.TryGetProperty("crossOrigin", out var crossOrigin) && crossOrigin.ValueKind != JsonValueKind.False))
                throw new PasskeyVerificationException();
        }
        catch (JsonException) { throw new PasskeyVerificationException(); }
    }
    private static PasskeyItem ToItem(PasskeyCredential value) => new(value.Id, value.Name, value.CreatedAtUtc, value.LastUsedAtUtc);
}
