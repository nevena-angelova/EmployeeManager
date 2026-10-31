namespace EmployeeManager.Application.Employees.OrgChart;

/// <summary>Flat employee row used as input for building the org chart.</summary>
public sealed record OrgChartEmployee(
    Guid Id,
    Guid? ManagerId,
    string FirstName,
    string LastName,
    string JobTitle,
    string Email);

/// <summary>
/// Builds a nested reporting tree from a flat list of employees. Each level is ordered by first name, then last name.
/// </summary>
public static class OrgChartBuilder
{
    /// <summary>
    /// Returns the top-level employees with their reports, or the single subtree rooted at <paramref name="rootId"/>.
    /// Returns null if <paramref name="rootId"/> is given but not found.
    /// </summary>
    public static IReadOnlyList<OrgChartNodeDto>? Build(IEnumerable<OrgChartEmployee> employees, Guid? rootId = null)
    {
        var list = employees as IReadOnlyCollection<OrgChartEmployee> ?? employees.ToList();
        var reportsByManager = list
            .Where(e => e.ManagerId is not null)
            .ToLookup(e => e.ManagerId!.Value);

        // Only self-reference is blocked in the DB, so longer cycles (A -> B -> A) are cut off here.
        var path = new HashSet<Guid>();

        if (rootId is { } id)
        {
            var root = list.FirstOrDefault(e => e.Id == id);
            return root is null ? null : [BuildNode(root, reportsByManager, path)];
        }

        return BuildLevel(list.Where(e => e.ManagerId is null), reportsByManager, path);
    }

    private static List<OrgChartNodeDto> BuildLevel(
        IEnumerable<OrgChartEmployee> employees,
        ILookup<Guid, OrgChartEmployee> reportsByManager,
        HashSet<Guid> path) =>
        employees
            .Where(e => !path.Contains(e.Id))
            .OrderBy(e => e.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.LastName, StringComparer.OrdinalIgnoreCase)
            .Select(e => BuildNode(e, reportsByManager, path))
            .ToList();

    private static OrgChartNodeDto BuildNode(
        OrgChartEmployee employee,
        ILookup<Guid, OrgChartEmployee> reportsByManager,
        HashSet<Guid> path)
    {
        path.Add(employee.Id);
        var directReports = BuildLevel(reportsByManager[employee.Id], reportsByManager, path);
        path.Remove(employee.Id);

        return new OrgChartNodeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.JobTitle,
            employee.Email,
            directReports);
    }
}
