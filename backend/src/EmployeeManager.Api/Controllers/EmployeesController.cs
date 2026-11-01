using System.Security.Claims;
using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Application.Employees.OrgChart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EmployeeManager.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController(
    IOrgChartService orgChartService,
    IEmployeeManagementService employeeService,
    IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Returns the reporting hierarchy as a nested tree, from all top-level managers or from <paramref name="rootId"/>.
    /// </summary>
    [HttpGet("org-chart")]
    [ProducesResponseType<IReadOnlyList<OrgChartNodeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrgChartNodeDto>>> GetOrgChart(
        [FromQuery] Guid? rootId,
        CancellationToken cancellationToken)
    {
        var orgChart = await orgChartService.GetOrgChartAsync(rootId, cancellationToken);
        return orgChart is null ? NotFound() : Ok(orgChart);
    }

    /// <summary>
    /// Lists every employee with their manager and role, sorted by first name, then last name.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EmployeeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> List(CancellationToken cancellationToken) =>
        Ok(await employeeService.ListAsync(cancellationToken));

    /// <summary>
    /// Returns one employee.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var employee = await employeeService.GetAsync(id, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    /// <summary>
    /// Creates an employee and their sign-in account with the given password and role.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await employeeService.CreateAsync(request, cancellationToken);
        return result.Status == EmployeeResultStatus.Success
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : ToErrorResponse(result);
    }

    /// <summary>
    /// Updates an employee's details, manager and role. Role changes apply from the user's next sign-in.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (await GetCurrentEmployeeIdAsync(cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await employeeService.UpdateAsync(id, request, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? Ok(result.Value) : ToErrorResponse(result);
    }

    /// <summary>
    /// Deletes an employee and their account. Employees with direct reports, and your own account, can't be deleted.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (await GetCurrentEmployeeIdAsync(cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await employeeService.DeleteAsync(id, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? NoContent() : ToErrorResponse(result);
    }

    private async Task<Guid?> GetCurrentEmployeeIdAsync(CancellationToken cancellationToken) =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? (await authService.GetCurrentUserAsync(userId, cancellationToken))?.EmployeeId
            : null;

    private ActionResult ToErrorResponse(EmployeeResult result)
    {
        switch (result.Status)
        {
            case EmployeeResultStatus.NotFound:
                return NotFound();
            case EmployeeResultStatus.Conflict:
                return Problem(detail: result.Detail, statusCode: StatusCodes.Status409Conflict);
            default:
                foreach (var (field, messages) in result.Errors)
                {
                    foreach (var message in messages)
                    {
                        ModelState.AddModelError(field, message);
                    }
                }

                return ValidationProblem(ModelState);
        }
    }
}
