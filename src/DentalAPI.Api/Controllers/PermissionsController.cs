using DentalAPI.Api.Authentication;
using DentalAPI.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[Produces("application/json")]
[Authorize(Policy = Policies.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class PermissionsController(IPermissionManagementService permissionService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List every permission code with its description")]
    [ProducesResponseType<IReadOnlyList<PermissionDto>>(StatusCodes.Status200OK)]
    public Ok<IReadOnlyList<PermissionDto>> List() => TypedResults.Ok(permissionService.ListPermissions());
}
