using System.Text.Json;

namespace DiaperScout.Application;

public sealed record PasskeyOptions(Guid RequestId, JsonElement PublicKey);
public sealed record BeginPasskeyRequest(string Binding);
public sealed record CompletePasskeyRequest(Guid RequestId, string Binding, JsonElement Credential, string? Name = null);
public sealed record PasskeyItem(Guid Id, string Name, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);

public interface IPasskeyAuthentication
{
    Task<PasskeyOptions> BeginRegistrationAsync(AuthenticatedUser actor, string binding, CancellationToken cancellationToken);
    Task<PasskeyItem> CompleteRegistrationAsync(AuthenticatedUser actor, CompletePasskeyRequest request, CancellationToken cancellationToken);
    Task<PasskeyOptions> BeginSignInAsync(string binding, CancellationToken cancellationToken);
    Task<PasswordlessAuthenticationResult> CompleteSignInAsync(CompletePasskeyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PasskeyItem>> GetPasskeysAsync(AuthenticatedUser actor, CancellationToken cancellationToken);
    Task<bool> RemovePasskeyAsync(AuthenticatedUser actor, Guid id, CancellationToken cancellationToken);
}

public sealed class PasskeyVerificationException : Exception
{
    public PasskeyVerificationException() : base("The passkey could not be verified. Please try again or use an email sign-in link.") { }
}
