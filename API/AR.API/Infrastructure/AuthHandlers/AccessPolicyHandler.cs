using Microsoft.AspNetCore.Authorization;

namespace AR.API.Infrastructure.AuthHandlers;

internal sealed class AccessPolicyRequirement: IAuthorizationRequirement;

internal sealed class AccessPolicyHandler : AuthorizationHandler<AccessPolicyRequirement>
{
    internal const string Name = nameof(AccessPolicyHandler);

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessPolicyRequirement requirement)
    {
        // TODO: implement access policy handler
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
