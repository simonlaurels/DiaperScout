using DiaperScout.Application;

namespace DiaperScout.Api;

public static class PasskeyEndpoints
{
    public static void MapPasskeyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var signIn = endpoints.MapGroup("/api/v1/auth/passkeys").AllowAnonymous().AddEndpointFilter<PasskeyErrors>();
        signIn.MapPost("/options", (BeginPasskeyRequest request, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            passkeys.BeginSignInAsync(request.Binding, ct));
        signIn.MapPost("/verify", (CompletePasskeyRequest request, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            passkeys.CompleteSignInAsync(request, ct));

        var account = endpoints.MapGroup("/api/v1/account/passkeys").RequireAuthorization().AddEndpointFilter<PasskeyErrors>();
        account.MapGet("/", async (ICurrentUser current, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            await passkeys.GetPasskeysAsync(await Actor(current, ct), ct));
        account.MapPost("/options", async (BeginPasskeyRequest request, ICurrentUser current, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            await passkeys.BeginRegistrationAsync(await Actor(current, ct), request.Binding, ct));
        account.MapPost("/verify", async (CompletePasskeyRequest request, ICurrentUser current, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            await passkeys.CompleteRegistrationAsync(await Actor(current, ct), request, ct));
        account.MapDelete("/{id:guid}", async (Guid id, ICurrentUser current, IPasskeyAuthentication passkeys, CancellationToken ct) =>
            await passkeys.RemovePasskeyAsync(await Actor(current, ct), id, ct) ? Results.NoContent() : Results.NotFound());
    }

    private static async Task<AuthenticatedUser> Actor(ICurrentUser current, CancellationToken ct) =>
        await current.GetAsync(ct) ?? throw new UnauthorizedAccessException();

    private sealed class PasskeyErrors : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try { return await next(context); }
            catch (PasskeyVerificationException) { return Results.BadRequest(new { message = "The passkey could not be verified. Try again or use an email sign-in link." }); }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
        }
    }
}
