using DiaperScout.Application;
using Microsoft.AspNetCore.Authorization;

namespace DiaperScout.Api;

public sealed class PublishAtlasRequirement : IAuthorizationRequirement;

public sealed class PublishAtlasAuthorizationHandler(ICurrentUser currentUser, IEditorialAuthorisation editorialAuthorisation)
    : AuthorizationHandler<PublishAtlasRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PublishAtlasRequirement requirement)
    {
        var user = await currentUser.GetAsync();
        if (user is not null && await editorialAuthorisation.CanPublishAtlasAsync(user)) context.Succeed(requirement);
    }
}
