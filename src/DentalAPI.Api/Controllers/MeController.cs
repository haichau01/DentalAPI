using DentalAPI.Api.Authentication;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.Controllers;

/// <summary>
/// The signed-in patient's own data. The patient id always comes from the access token; future
/// /me/appointments, /me/invoices and /me/treatment-records endpoints follow the same rule.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Produces("application/json")]
[Authorize(Policy = Policies.PatientSelf)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class MeController(
    ICurrentPatientService currentPatientService,
    IPatientEmailVerificationService emailVerificationService) : ControllerBase
{
    [HttpGet("profile")]
    [EndpointSummary("Get the signed-in patient's own record")]
    [ProducesResponseType<PatientDto>(StatusCodes.Status200OK)]
    public async Task<Ok<PatientDto>> GetProfile(CancellationToken cancellationToken)
    {
        var patient = await currentPatientService.GetProfileAsync(cancellationToken);
        return TypedResults.Ok(patient);
    }

    [HttpPut("address")]
    [EndpointSummary("Change the signed-in patient's address (null clears it)")]
    [ProducesResponseType<PatientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Ok<PatientDto>> UpdateAddress(UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        var patient = await currentPatientService.UpdateAddressAsync(request, cancellationToken);
        return TypedResults.Ok(patient);
    }

    [HttpPost("email/send-code")]
    [EndpointSummary("Set a sign-in email and send a verification code to it")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<Accepted> SendEmailCode(SendEmailVerificationCodeRequest request, CancellationToken cancellationToken)
    {
        await emailVerificationService.SendCodeAsync(request, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    [HttpPost("email/verify")]
    [EndpointSummary("Verify the email with the code so it can be used to sign in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await emailVerificationService.VerifyAsync(request, cancellationToken);
        return TypedResults.NoContent();
    }
}
