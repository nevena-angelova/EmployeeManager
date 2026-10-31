namespace EmployeeManager.Application.Employees.OrgChart;

public interface IOrgChartService
{
    /// <summary>
    /// Returns the reporting tree, either from all top-level managers or from <paramref name="rootId"/>.
    /// Returns null if <paramref name="rootId"/> doesn't match an employee.
    /// </summary>
    Task<IReadOnlyList<OrgChartNodeDto>?> GetOrgChartAsync(Guid? rootId, CancellationToken cancellationToken = default);
}
