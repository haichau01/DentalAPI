using DentalAPI.Api.Authentication;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.StaffAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/staff-accounts")]
[Produces("application/json")]
[Authorize(Policy = Policies.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class StaffAccountsController(
    IStaffAccountService staffAccountService,
    IPermissionManagementService permissionService) : ControllerBase
{
    private const string GetStaffAccountRouteName = "GetStaffAccount";

    [HttpGet]
    [EndpointSummary("List staff accounts, ordered by full name")]
    [ProducesResponseType<PagedResult<StaffAccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Ok<PagedResult<StaffAccountDto>>> List([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var accounts = await staffAccountService.ListAsync(query, cancellationToken);
        return TypedResults.Ok(accounts);
    }

    [HttpGet("{id:guid}", Name = GetStaffAccountRouteName)]
    [EndpointSummary("Get a staff account by id")]
    [ProducesResponseType<StaffAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Ok<StaffAccountDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await staffAccountService.GetByIdAsync(id, cancellationToken);
        return TypedResults.Ok(account);
    }

    [HttpPost]
    [EndpointSummary("Create a dentist or receptionist account; they must change the initial password at first sign-in")]
    [ProducesResponseType<StaffAccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRoute<StaffAccountDto>> Create(CreateStaffAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await staffAccountService.CreateAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(account, GetStaffAccountRouteName, new { id = account.Id });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a staff member's name, phone and address")]
    [ProducesResponseType<StaffAccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Ok<StaffAccountDto>> UpdateProfile(Guid id, UpdateStaffProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await staffAccountService.UpdateProfileAsync(id, request, cancellationToken);
        return TypedResults.Ok(account);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a staff account (soft delete) and sign it out everywhere")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> Delete(Guid id, CancellationToken cancellationToken)
    {
        await staffAccountService.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    [EndpointSummary("Get a staff member's effective permissions and individual overrides")]
    [ProducesResponseType<StaffAccountPermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Ok<StaffAccountPermissionsDto>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetAccountPermissionsAsync(id, cancellationToken);
        return TypedResults.Ok(permissions);
    }

    [HttpPut("{id:guid}/permission-overrides")]
    [EndpointSummary("Replace the permissions granted to or revoked from one staff member")]
    [ProducesResponseType<StaffAccountPermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Ok<StaffAccountPermissionsDto>> ReplaceOverrides(
        Guid id, ReplacePermissionOverridesRequest request, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.ReplaceOverridesAsync(id, request, cancellationToken);
        return TypedResults.Ok(permissions);
    }
}
