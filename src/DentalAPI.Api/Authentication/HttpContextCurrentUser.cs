using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Api.Authentication;

/// <summary>Reads the caller from the validated access token of the current HTTP request.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? AccountId => ReadGuid(AccessTokenClaims.Subject);

    public AccountType? AccountType =>
        Enum.TryParse<AccountType>(ReadClaim(AccessTokenClaims.AccountType), out var accountType) ? accountType : null;

    public Guid? PatientId => ReadGuid(AccessTokenClaims.PatientId);

    private string? ReadClaim(string claimType)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? user.FindFirst(claimType)?.Value : null;
    }

    private Guid? ReadGuid(string claimType) => Guid.TryParse(ReadClaim(claimType), out var value) ? value : null;
}
