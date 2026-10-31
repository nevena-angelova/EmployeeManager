using EmployeeManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Application.Employees.OrgChart;

public class OrgChartService(IApplicationDbContext db) : IOrgChartService
{
    public async Task<IReadOnlyList<OrgChartNodeDto>?> GetOrgChartAsync(Guid? rootId, CancellationToken cancellationToken = default)
    {
        // Load the whole hierarchy in one query and assemble the tree in memory.
        var employees = await db.Employees
            .AsNoTracking()
            .Select(e => new OrgChartEmployee(e.Id, e.ManagerId, e.FirstName, e.LastName, e.JobTitle, e.Email))
            .ToListAsync(cancellationToken);

        return OrgChartBuilder.Build(employees, rootId);
    }
}
