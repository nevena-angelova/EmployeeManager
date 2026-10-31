using EmployeeManager.Application.Employees.OrgChart;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController(IOrgChartService orgChartService) : ControllerBase
{
    /// <summary>
    /// Returns the reporting hierarchy as a nested tree, from all top-level managers or from <paramref name="rootId"/>.
    /// </summary>
    [HttpGet("org-chart")]
    [ProducesResponseType<IReadOnlyList<OrgChartNodeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrgChartNodeDto>>> GetOrgChart(
        [FromQuery] Guid? rootId,
        CancellationToken cancellationToken)
    {
        var orgChart = await orgChartService.GetOrgChartAsync(rootId, cancellationToken);
        return orgChart is null ? NotFound() : Ok(orgChart);
    }
}
