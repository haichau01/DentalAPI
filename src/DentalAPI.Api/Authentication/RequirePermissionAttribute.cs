using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DentalAPI.Api.Authentication;

/// <summary>Allows only staff who currently hold the permission (admins hold all of them).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(Permission permission) : AuthorizeAttribute, IAuthorizationRequirementData
{
    public Permission Permission { get; } = permission;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new DenyAnonymousAuthorizationRequirement();
        yield return new ClaimsAuthorizationRequirement(AccessTokenClaims.AccountType, [nameof(AccountType.Staff)]);
        yield return new PasswordChangeCompletedRequirement();
        yield return new PermissionRequirement(Permission);
    }
}
