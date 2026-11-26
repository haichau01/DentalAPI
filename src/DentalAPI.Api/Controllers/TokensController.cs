using DentalAPI.Api.RateLimiting;
using DentalAPI.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class TokensController(IRefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.PublicAuthPolicy)]
    [EndpointSummary("Exchange a refresh token for a new token pair (the old refresh token stops working)")]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<Ok<AuthTokensDto>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokens = await refreshTokenService.RefreshAsync(request, cancellationToken);
        return TypedResults.Ok(tokens);
    }

    [HttpPost("logout")]
    [EndpointSummary("Revoke the caller's refresh token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<NoContent> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await refreshTokenService.LogoutAsync(request, cancellationToken);
        return TypedResults.NoContent();
    }
}
