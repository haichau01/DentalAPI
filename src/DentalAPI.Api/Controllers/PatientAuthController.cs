using DentalAPI.Api.RateLimiting;
using DentalAPI.Application.Auth;
using DentalAPI.Application.PatientAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/auth/patients")]
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingSetup.PublicAuthPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class PatientAuthController(
    IPatientRegistrationService registrationService,
    IPatientLoginService loginService) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Register a patient account; a verification code is sent by SMS")]
    [ProducesResponseType<PatientRegistrationDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Accepted<PatientRegistrationDto>> Register(RegisterPatientAccountRequest request, CancellationToken cancellationToken)
    {
        var registration = await registrationService.RegisterAsync(request, cancellationToken);
        return TypedResults.Accepted((string?)null, registration);
    }

    [HttpPost("verify-phone")]
    [EndpointSummary("Verify the phone with the SMS code, activate the account and sign in")]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Ok<AuthTokensDto>> VerifyPhone(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        var tokens = await registrationService.VerifyPhoneAsync(request, cancellationToken);
        return TypedResults.Ok(tokens);
    }

    [HttpPost("resend-otp")]
    [EndpointSummary("Send a new verification code to a pending registration")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Accepted> ResendCode(ResendVerificationCodeRequest request, CancellationToken cancellationToken)
    {
        await registrationService.ResendCodeAsync(request, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    [HttpPost("login")]
    [EndpointSummary("Sign in with phone number or verified email")]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<Ok<AuthTokensDto>> Login(PatientLoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await loginService.LoginAsync(request, cancellationToken);
        return TypedResults.Ok(tokens);
    }
}
