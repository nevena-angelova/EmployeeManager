using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectService projectService, IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Lists the projects the signed-in manager owns, sorted by name.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpGet("managed")]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> ListManaged(CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        return Ok(await projectService.ListManagedAsync(currentEmployeeId, cancellationToken));
    }

    /// <summary>
    /// Lists the projects the signed-in employee is a member of, sorted by name.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> ListMine(CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        return Ok(await projectService.ListMineAsync(currentEmployeeId, cancellationToken));
    }

    /// <summary>
    /// Returns a project with its members. Only its owner and members can see it; anyone else gets 404.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var project = await projectService.GetAsync(id, currentEmployeeId, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    /// <summary>
    /// Creates a project owned by the signed-in manager, staffed from their direct reports.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpPost]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDto>> Create(SaveProjectRequest request, CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await projectService.CreateAsync(request, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : this.ToErrorResponse(result);
    }

    /// <summary>
    /// Updates one of the signed-in manager's projects and replaces its members. Members who no longer report to the
    /// manager can stay, but new members must be direct reports.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Update(
        Guid id,
        SaveProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await projectService.UpdateAsync(id, request, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? Ok(result.Value) : this.ToErrorResponse(result);
    }

    /// <summary>
    /// Deletes one of the signed-in manager's projects.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await projectService.DeleteAsync(id, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? NoContent() : this.ToErrorResponse(result);
    }
}
