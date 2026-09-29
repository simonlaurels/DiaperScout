using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PasskeyApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private const string Account = "/api/v1/account/passkeys";
    private const string SignIn = "/api/v1/auth/passkeys";
    private static string Binding() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterSignInListRemove_UsesExistingAccountAndCurrentRoles(bool administrator)
    {
        var user = await CreateUser(administrator);
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, user);
        using var anonymous = factory.CreateClient();
        using var key = new TestPasskey();
        var binding = Binding();
        var registration = await Options(owner, Account, binding);
        Assert.Equal("required", registration.PublicKey.GetProperty("authenticatorSelection").GetProperty("userVerification").GetString());
        Assert.Equal("required", registration.PublicKey.GetProperty("authenticatorSelection").GetProperty("residentKey").GetString());
        var created = await owner.PostAsJsonAsync(Account + "/verify", new CompletePasskeyRequest(registration.RequestId, binding, key.Register(registration), "My phone"));
        created.EnsureSuccessStatusCode();
        var item = (await created.Content.ReadFromJsonAsync<PasskeyItem>())!;
        Assert.Equal("My phone", item.Name);
        Assert.Single((await owner.GetFromJsonAsync<PasskeyItem[]>(Account + "/"))!);
        var options = await Options(anonymous, SignIn, binding);
        Assert.Empty(options.PublicKey.GetProperty("allowCredentials").EnumerateArray());
        var request = new CompletePasskeyRequest(options.RequestId, binding, key.Assert(options));
        var signedIn = await anonymous.PostAsJsonAsync(SignIn + "/verify", request);
        signedIn.EnsureSuccessStatusCode();
        var result = (await signedIn.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>())!;
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Subject, result.Subject);
        Assert.Equal(administrator ? [PrivilegedRole.Administrator] : Array.Empty<PrivilegedRole>(), result.Roles);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync(SignIn + "/verify", request)).StatusCode);
        var used = Assert.Single((await owner.GetFromJsonAsync<PasskeyItem[]>(Account + "/"))!);
        Assert.NotNull(used.LastUsedAtUtc);

        using var stranger = Client(factory, await CreateUser());
        Assert.Empty((await stranger.GetFromJsonAsync<PasskeyItem[]>(Account + "/"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync(Account + "/" + item.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(Account + "/" + item.Id)).StatusCode);
        var afterRemoval = await Options(anonymous, SignIn, binding);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync(SignIn + "/verify",
            new CompletePasskeyRequest(afterRemoval.RequestId, binding, key.Assert(afterRemoval, 2)))).StatusCode);
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("rp")]
    [InlineData("signature")]
    [InlineData("uv")]
    [InlineData("presence")]
    [InlineData("handle")]
    [InlineData("crossOrigin")]
    [InlineData("binding")]
    [InlineData("expired")]
    [InlineData("challenge")]
    [InlineData("suspended")]
    [InlineData("backupEligibility")]
    public async Task SignIn_RejectsInvalidAssertions(string fault)
    {
        var user = await CreateUser();
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, user);
        using var anonymous = factory.CreateClient();
        using var key = new TestPasskey();
        var binding = Binding();
        await Register(owner, key, binding);
        var options = await Options(anonymous, SignIn, binding);
        if (fault == "expired")
        {
            await using var db = fixture.CreateDbContext();
            await db.PasskeyChallenges.Where(x => x.Id == options.RequestId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }
        if (fault == "suspended")
        {
            await using var db = fixture.CreateDbContext();
            await db.Users.Where(x => x.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, UserAccountStatus.Suspended));
        }
        var signedOptions = fault == "challenge" ? await Options(anonymous, SignIn, binding) : options;
        var credential = key.Assert(signedOptions,
            origin: fault == "origin" ? "https://evil.example" : "https://localhost:7167",
            rpId: fault == "rp" ? "evil.example" : "localhost",
            flags: (byte)(fault == "uv" ? 1 : fault == "presence" ? 4 : fault == "backupEligibility" ? 13 : 5),
            badSignature: fault == "signature", crossOrigin: fault == "crossOrigin",
            userHandle: fault == "handle" ? Guid.NewGuid().ToByteArray() : null);
        var result = await anonymous.PostAsJsonAsync(SignIn + "/verify", new CompletePasskeyRequest(options.RequestId,
            fault == "binding" ? Binding() : binding, credential));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("origin")]
    [InlineData("uv")]
    [InlineData("binding")]
    [InlineData("ceremony")]
    [InlineData("malformed")]
    public async Task Registration_RejectsInvalidOrMismatchedRequests(string fault)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, await CreateUser());
        using var stranger = Client(factory, await CreateUser());
        using var key = new TestPasskey();
        var binding = Binding();
        var options = await Options(owner, Account, binding);
        var credential = fault == "malformed" ? JsonSerializer.SerializeToElement(new { }) : key.Register(options,
            origin: fault == "origin" ? "https://evil.example" : "https://localhost:7167", verified: fault != "uv");
        var result = await (fault == "owner" ? stranger : owner).PostAsJsonAsync(
            (fault == "ceremony" ? SignIn : Account) + "/verify",
            new CompletePasskeyRequest(options.RequestId, fault == "binding" ? Binding() : binding, credential, "Test"));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Registration_RequiresAuthenticationAndCannotReplayOrDuplicateCredential()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Account + "/options", new BeginPasskeyRequest(Binding()))).StatusCode);
        using var owner = Client(factory, await CreateUser());
        using var key = new TestPasskey();
        var binding = Binding();
        var options = await Options(owner, Account, binding);
        var request = new CompletePasskeyRequest(options.RequestId, binding, key.Register(options), "First");
        (await owner.PostAsJsonAsync(Account + "/verify", request)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Account + "/verify", request)).StatusCode);
        var second = await Options(owner, Account, binding);
        Assert.Single(second.PublicKey.GetProperty("excludeCredentials").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Account + "/verify",
            new CompletePasskeyRequest(second.RequestId, binding, key.Register(second), "Duplicate"))).StatusCode);
    }

    [Fact]
    public async Task SignIn_ConcurrentReplaySucceedsOnce_AndCounterCannotDecrease()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, await CreateUser());
        using var anonymous = factory.CreateClient();
        using var key = new TestPasskey();
        var binding = Binding();
        await Register(owner, key, binding);
        var options = await Options(anonymous, SignIn, binding);
        var request = new CompletePasskeyRequest(options.RequestId, binding, key.Assert(options, 3));
        var responses = await Task.WhenAll(anonymous.PostAsJsonAsync(SignIn + "/verify", request), anonymous.PostAsJsonAsync(SignIn + "/verify", request));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
        var next = await Options(anonymous, SignIn, binding);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync(SignIn + "/verify",
            new CompletePasskeyRequest(next.RequestId, binding, key.Assert(next, 2)))).StatusCode);
    }

    [Fact]
    public async Task MagicLink_StillSignsInSameAccountAfterPasskeyIsAdded()
    {
        var user = await CreateUser(true);
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, user);
        using var key = new TestPasskey();
        await Register(owner, key, Binding());
        var token = Binding();
        await using (var db = fixture.CreateDbContext())
        {
            var email = await db.UserEmails.SingleAsync(x => x.UserId == user.Id);
            db.MagicLinkTokens.Add(new MagicLinkToken(email.Id,
                Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant(), DateTimeOffset.UtcNow.AddMinutes(5)));
            await db.SaveChangesAsync();
        }
        using var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsync("/api/v1/auth/magic-link/consume?token=" + token, null);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>())!;
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal([PrivilegedRole.Administrator], result.Roles);
    }

    [Fact]
    public async Task SyncedPasskey_AllowsZeroCounters_AndReloadsRolesOnEverySignIn()
    {
        var user = await CreateUser(true);
        using var factory = new ObservationApiFactory(fixture);
        using var owner = Client(factory, user);
        using var anonymous = factory.CreateClient();
        using var key = new TestPasskey();
        var binding = Binding();
        var registration = await Options(owner, Account, binding);
        (await owner.PostAsJsonAsync(Account + "/verify", new CompletePasskeyRequest(registration.RequestId, binding,
            key.Register(registration, backupEligible: true), "Synced passkey"))).EnsureSuccessStatusCode();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (attempt == 1)
            {
                await using var db = fixture.CreateDbContext();
                var role = await db.PrivilegedRoleAssignments.SingleAsync(x => x.UserId == user.Id && x.Role == PrivilegedRole.Administrator);
                role.Revoke(fixture.AdministratorUserId, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync();
            }
            var options = await Options(anonymous, SignIn, binding);
            var response = await anonymous.PostAsJsonAsync(SignIn + "/verify",
                new CompletePasskeyRequest(options.RequestId, binding, key.Assert(options, counter: 0, flags: 29)));
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>())!;
            Assert.Equal(attempt == 0 ? [PrivilegedRole.Administrator] : Array.Empty<PrivilegedRole>(), result.Roles);
        }
    }

    private async Task<User> CreateUser(bool administrator = false)
    {
        var user = new User("passkey-" + Guid.NewGuid().ToString("N"));
        await using var db = fixture.CreateDbContext();
        db.AddRange(user, new UserEmail(user.Id, user.Subject + "@example.test"));
        if (administrator) db.Add(new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Administrator, fixture.AdministratorUserId, DateTimeOffset.UtcNow));
        var revoked = new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Moderator, fixture.AdministratorUserId, DateTimeOffset.UtcNow.AddDays(-1));
        revoked.Revoke(fixture.AdministratorUserId, DateTimeOffset.UtcNow);
        db.Add(revoked);
        await db.SaveChangesAsync();
        return user;
    }
    private static HttpClient Client(ObservationApiFactory factory, User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", user.Subject);
        return client;
    }
    private static async Task<PasskeyOptions> Options(HttpClient client, string path, string binding)
    {
        var response = await client.PostAsJsonAsync(path + "/options", new BeginPasskeyRequest(binding));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PasskeyOptions>())!;
    }
    private static async Task Register(HttpClient owner, TestPasskey key, string binding)
    {
        var options = await Options(owner, Account, binding);
        (await owner.PostAsJsonAsync(Account + "/verify", new CompletePasskeyRequest(options.RequestId, binding, key.Register(options), "Test passkey"))).EnsureSuccessStatusCode();
    }
}
