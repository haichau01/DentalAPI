using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace DentalAPI.Api.Authentication;

/// <summary>Blocks staff who still have to replace the password an admin chose for them.</summary>
public sealed class PasswordChangeCompletedRequirement : IAuthorizationRequirement;

public sealed class PasswordChangeCompletedHandler : AuthorizationHandler<PasswordChangeCompletedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordChangeCompletedRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.User.HasClaim(claim => claim.Type == AccessTokenClaims.PasswordChangeRequired))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed record PermissionRequirement(Permission Permission) : IAuthorizationRequirement;

/// <summary>
/// Checks the permission against the current database state (through a short cache) rather than the token,
/// so an admin's change applies on the next request instead of after the token expires.
/// </summary>
public sealed class PermissionAuthorizationHandler(IEffectivePermissionProvider permissionProvider)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirst(AccessTokenClaims.Subject)?.Value, out var staffAccountId))
        {
            return;
        }

        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        var permissions = await permissionProvider.GetAsync(staffAccountId, cancellationToken);
        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
