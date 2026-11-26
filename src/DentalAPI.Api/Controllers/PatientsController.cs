using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Patients;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Produces("application/json")]
public sealed class PatientsController(IPatientService patientService) : ControllerBase
{
    private const string GetPatientRouteName = "GetPatient";

    [HttpGet]
    [EndpointSummary("List patients, ordered by full name")]
    [ProducesResponseType<PagedResult<PatientDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Ok<PagedResult<PatientDto>>> List([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var patients = await patientService.ListAsync(query, cancellationToken);
        return TypedResults.Ok(patients);
    }

    [HttpGet("{id:guid}", Name = GetPatientRouteName)]
    [EndpointSummary("Get a patient by id")]
    [ProducesResponseType<PatientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Ok<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetByIdAsync(id, cancellationToken);
        return TypedResults.Ok(patient);
    }

    [HttpPost]
    [EndpointSummary("Create a patient")]
    [ProducesResponseType<PatientDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<CreatedAtRoute<PatientDto>> Create(SavePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await patientService.CreateAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(patient, GetPatientRouteName, new { id = patient.Id });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a patient's details")]
    [ProducesResponseType<PatientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Ok<PatientDto>> Update(Guid id, SavePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await patientService.UpdateAsync(id, request, cancellationToken);
        return TypedResults.Ok(patient);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a patient (soft delete; history is kept)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContent> Delete(Guid id, CancellationToken cancellationToken)
    {
        await patientService.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
