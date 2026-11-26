using DentalAPI.Api.Authentication;
using DentalAPI.Application.Authorization;
using DentalAPI.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/staff-roles/{role}/permissions")]
[Produces("application/json")]
[Authorize(Policy = Policies.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class StaffRolesController(IPermissionManagementService permissionService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get the permissions every staff member of a role has (Admin, Dentist or Receptionist)")]
    [ProducesResponseType<StaffRolePermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Ok<StaffRolePermissionsDto>> Get(StaffRole role, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetRolePermissionsAsync(role, cancellationToken);
        return TypedResults.Ok(permissions);
    }

    [HttpPut]
    [EndpointSummary("Replace the permissions of a role; applies to its staff on their next request")]
    [ProducesResponseType<StaffRolePermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Ok<StaffRolePermissionsDto>> Replace(
        StaffRole role, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.ReplaceRolePermissionsAsync(role, request, cancellationToken);
        return TypedResults.Ok(permissions);
    }
}
