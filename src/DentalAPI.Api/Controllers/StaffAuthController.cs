using DentalAPI.Api.RateLimiting;
using DentalAPI.Application.Auth;
using DentalAPI.Application.StaffAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/auth/staff")]
[Produces("application/json")]
public sealed class StaffAuthController(IStaffLoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.PublicAuthPolicy)]
    [EndpointSummary("Staff sign-in with email")]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<Ok<AuthTokensDto>> Login(StaffLoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await loginService.LoginAsync(request, cancellationToken);
        return TypedResults.Ok(tokens);
    }

    [HttpPost("change-password")]
    [EndpointSummary("Change the signed-in staff member's password; other sessions are signed out")]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<Ok<AuthTokensDto>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var tokens = await loginService.ChangePasswordAsync(request, cancellationToken);
        return TypedResults.Ok(tokens);
    }
}
