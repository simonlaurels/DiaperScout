using DiaperScout.Application;

namespace DiaperScout.Api;

public static class UserManagementEndpoints
{
    public static IEndpointRouteBuilder MapUserManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/v1/user-management/users",
            async (
                ICurrentUser currentUser,
                IUserManagement userManagement,
                CancellationToken cancellationToken) =>
            {
                var actor = await currentUser.GetAsync(cancellationToken);
                if (actor is null)
                    return Results.Forbid();

                try
                {
                    return Results.Ok(await userManagement.GetUsersAsync(actor, cancellationToken));
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .WithName("GetUserManagementUsers")
            .WithTags("User Management")
            .Produces<IReadOnlyList<UserManagementItem>>();

        endpoints.MapGet(
            "/api/v1/user-management/registration",
            async (
                ICurrentUser currentUser,
                IUserManagement userManagement,
                CancellationToken cancellationToken) =>
            {
                var actor = await currentUser.GetAsync(cancellationToken);
                if (actor is null)
                    return Results.Forbid();

                try
                {
                    return Results.Ok(await userManagement.GetRegistrationSettingsAsync(actor, cancellationToken));
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .WithName("GetRegistrationSettings")
            .WithTags("User Management")
            .Produces<RegistrationSettingsItem>();

        endpoints.MapPut(
            "/api/v1/user-management/registration",
            async (
                SetRegistrationEnabledRequest request,
                ICurrentUser currentUser,
                IUserManagement userManagement,
                CancellationToken cancellationToken) =>
            {
                var actor = await currentUser.GetAsync(cancellationToken);
                if (actor is null)
                    return Results.Forbid();

                try
                {
                    return Results.Ok(await userManagement.SetRegistrationEnabledAsync(actor, request.Enabled, cancellationToken));
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .WithName("SetRegistrationEnabled")
            .WithTags("User Management")
            .Produces<RegistrationSettingsItem>();

        return endpoints;
    }
}

public sealed record SetRegistrationEnabledRequest(bool Enabled);