using EmployeeManager.Application.Allocations;
using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.Api.Controllers;

/// <summary>
/// Monthly hours on projects. <c>month</c> is <c>yyyy-MM</c> and defaults to the current month (UTC).
/// </summary>
[ApiController]
[Route("api/allocations")]
public class AllocationsController(
    IAllocationService allocationService,
    IAuthService authService,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the signed-in manager's direct reports and their hours on the manager's projects in the month.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpGet("team")]
    [ProducesResponseType<TeamAllocationsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TeamAllocationsDto>> GetTeam(string? month, CancellationToken cancellationToken)
    {
        if (ParseMonth(month) is not { } parsed)
        {
            return InvalidMonth();
        }

        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        return Ok(await allocationService.GetTeamAsync(parsed, currentEmployeeId, cancellationToken));
    }

    /// <summary>
    /// Replaces the month's hours for the signed-in manager's direct reports on the manager's projects. Anything left
    /// out, or given 0 hours, is removed. Each employee's total, including hours on other projects, must fit the
    /// month's capacity. Past months give 409.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ManagerOnly)]
    [HttpPut("team")]
    [ProducesResponseType<TeamAllocationsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamAllocationsDto>> SaveTeam(
        string? month,
        SaveTeamAllocationsRequest request,
        CancellationToken cancellationToken)
    {
        if (ParseMonth(month) is not { } parsed)
        {
            return InvalidMonth();
        }

        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await allocationService.SaveTeamAsync(parsed, request, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? Ok(result.Value) : this.ToErrorResponse(result);
    }

    /// <summary>
    /// Returns the signed-in employee's hours in the month on every project they're on or have hours on.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType<MyAllocationsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MyAllocationsDto>> GetMine(string? month, CancellationToken cancellationToken)
    {
        if (ParseMonth(month) is not { } parsed)
        {
            return InvalidMonth();
        }

        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        return Ok(await allocationService.GetMineAsync(parsed, currentEmployeeId, cancellationToken));
    }

    /// <summary>The requested month, the current one if none was given, or null if it isn't <c>yyyy-MM</c>.</summary>
    private DateOnly? ParseMonth(string? month) =>
        string.IsNullOrEmpty(month) ? AllocationMonth.Current(timeProvider)
        : AllocationMonth.TryParse(month, out var parsed) ? parsed
        : null;

    private ActionResult InvalidMonth() =>
        this.ToErrorResponse(EmployeeResult.Invalid("month", "The month must look like 2026-11."));
}
