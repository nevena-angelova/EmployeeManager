using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Application.Allocations;

public class AllocationService(IApplicationDbContext db, TimeProvider timeProvider) : IAllocationService
{
    private const string PastMonthError = "Past months can't be changed.";

    private const string ConcurrentChangeError =
        "The hours, projects or team changed while you were saving. Reload the month and try again.";

    public async Task<TeamAllocationsDto> GetTeamAsync(
        DateOnly month,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == managerId)
            .OrderBy(p => p.Name)
            .Select(p => new AllocationProjectDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        var reports = await db.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == managerId)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.JobTitle })
            .ToListAsync(cancellationToken);

        var memberships = await db.ProjectMembers
            .AsNoTracking()
            .Where(m => m.Project!.OwnerId == managerId && m.Employee!.ManagerId == managerId)
            .Select(m => new { m.EmployeeId, m.ProjectId })
            .ToListAsync(cancellationToken);

        var allocations = await db.HourAllocations
            .AsNoTracking()
            .Where(a => a.Month == month && a.Employee!.ManagerId == managerId)
            .Select(a => new { a.EmployeeId, a.ProjectId, a.Hours, IsOwn = a.Project!.OwnerId == managerId })
            .ToListAsync(cancellationToken);

        var nextMonth = month.AddMonths(1);
        var logged = await db.TimeEntries
            .AsNoTracking()
            .Where(e => e.Date >= month && e.Date < nextMonth && e.Employee!.ManagerId == managerId)
            .GroupBy(e => new { e.EmployeeId, e.ProjectId, IsOwn = e.Project!.OwnerId == managerId })
            .Select(g => new { g.Key.EmployeeId, g.Key.ProjectId, g.Key.IsOwn, Hours = g.Sum(e => e.Hours) })
            .ToListAsync(cancellationToken);

        var projectOrder = projects.Select((p, index) => (p.Id, index)).ToDictionary(p => p.Id, p => p.index);
        var members = reports
            .Select(r =>
            {
                var own = allocations.Where(a => a.EmployeeId == r.Id && a.IsOwn).ToList();
                var ownLogged = logged.Where(e => e.EmployeeId == r.Id && e.IsOwn).ToList();
                return new TeamMemberAllocationDto(
                    r.Id,
                    r.FirstName,
                    r.LastName,
                    r.JobTitle,
                    memberships.Where(m => m.EmployeeId == r.Id).Select(m => m.ProjectId)
                        .Union(own.Select(a => a.ProjectId))
                        .OrderBy(id => projectOrder[id])
                        .ToList(),
                    own.OrderBy(a => projectOrder[a.ProjectId]).Select(a => new ProjectHoursDto(a.ProjectId, a.Hours)).ToList(),
                    allocations.Where(a => a.EmployeeId == r.Id && !a.IsOwn).Sum(a => a.Hours),
                    ownLogged.OrderBy(e => projectOrder[e.ProjectId]).Select(e => new ProjectHoursDto(e.ProjectId, e.Hours)).ToList(),
                    logged.Where(e => e.EmployeeId == r.Id && !e.IsOwn).Sum(e => e.Hours));
            })
            .ToList();

        return new TeamAllocationsDto(
            AllocationMonth.Format(month),
            AllocationMonth.CapacityHours(month),
            AllocationMonth.IsEditable(month, AllocationMonth.Current(timeProvider)),
            projects,
            members);
    }

    public async Task<EmployeeResult<TeamAllocationsDto>> SaveTeamAsync(
        DateOnly month,
        SaveTeamAllocationsRequest request,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        if (!AllocationMonth.IsEditable(month, AllocationMonth.Current(timeProvider)))
        {
            return EmployeeResult<TeamAllocationsDto>.From(EmployeeResult.Conflict(PastMonthError));
        }

        var items = request.Allocations ?? [];
        var reports = await db.Employees
            .Where(e => e.ManagerId == managerId)
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);
        if (AllocationRules.ValidateRequest(items, reports.Keys.ToHashSet()) is { } requestError)
        {
            return EmployeeResult<TeamAllocationsDto>.From(EmployeeResult.Invalid("allocations", requestError));
        }

        var memberships = (await db.ProjectMembers
                .Where(m => m.Project!.OwnerId == managerId && m.Employee!.ManagerId == managerId)
                .Select(m => new { m.EmployeeId, m.ProjectId })
                .ToListAsync(cancellationToken))
            .ToLookup(m => m.EmployeeId, m => m.ProjectId);
        var existing = await db.HourAllocations
            .Where(a => a.Month == month && a.Project!.OwnerId == managerId && a.Employee!.ManagerId == managerId)
            .ToListAsync(cancellationToken);
        var otherHours = await db.HourAllocations
            .Where(a => a.Month == month && a.Employee!.ManagerId == managerId && a.Project!.OwnerId != managerId)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Hours = g.Sum(a => a.Hours) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Hours, cancellationToken);

        var errors = new Dictionary<string, string[]>();
        foreach (var (employeeId, name) in reports)
        {
            var hours = items
                .Where(a => a.EmployeeId == employeeId)
                .Select(a => new ProjectHoursDto(a.ProjectId, a.Hours))
                .ToList();
            var error = AllocationRules.ValidateEmployee(
                name,
                hours,
                // Hours they already have on a project they've since left can stay or change, as GetTeamAsync offers.
                memberships[employeeId]
                    .Concat(existing.Where(a => a.EmployeeId == employeeId).Select(a => a.ProjectId))
                    .ToHashSet(),
                otherHours.GetValueOrDefault(employeeId),
                month);
            if (error is not null)
            {
                errors[$"allocations.{employeeId}"] = [error];
            }
        }

        if (errors.Count > 0)
        {
            return EmployeeResult<TeamAllocationsDto>.From(EmployeeResult.Invalid(errors));
        }

        var wanted = items.Where(a => a.Hours > 0).ToDictionary(a => (a.EmployeeId, a.ProjectId), a => a.Hours);
        foreach (var allocation in existing)
        {
            if (wanted.Remove((allocation.EmployeeId, allocation.ProjectId), out var hours))
            {
                allocation.Hours = hours;
            }
            else
            {
                db.HourAllocations.Remove(allocation);
            }
        }

        foreach (var ((employeeId, projectId), hours) in wanted)
        {
            db.HourAllocations.Add(new HourAllocation
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                Month = month,
                Hours = hours,
            });
        }

        // Fails on another save of the same month (the unique index or a row already removed), or a project or
        // employee being deleted meanwhile.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return EmployeeResult<TeamAllocationsDto>.From(EmployeeResult.Conflict(ConcurrentChangeError));
        }

        return EmployeeResult<TeamAllocationsDto>.Success(await GetTeamAsync(month, managerId, cancellationToken));
    }

    public async Task<MyAllocationsDto> GetMineAsync(
        DateOnly month,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var nextMonth = month.AddMonths(1);
        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.Members.Any(m => m.EmployeeId == employeeId)
                || db.HourAllocations.Any(a => a.ProjectId == p.Id && a.EmployeeId == employeeId && a.Month == month)
                || db.TimeEntries.Any(e =>
                    e.ProjectId == p.Id && e.EmployeeId == employeeId && e.Date >= month && e.Date < nextMonth))
            .OrderBy(p => p.Name)
            .Select(p => new MyProjectHoursDto(
                p.Id,
                p.Name,
                p.Owner!.FirstName + " " + p.Owner.LastName,
                db.HourAllocations
                    .Where(a => a.ProjectId == p.Id && a.EmployeeId == employeeId && a.Month == month)
                    .Sum(a => (decimal?)a.Hours) ?? 0,
                db.TimeEntries
                    .Where(e => e.ProjectId == p.Id && e.EmployeeId == employeeId && e.Date >= month && e.Date < nextMonth)
                    .Sum(e => (decimal?)e.Hours) ?? 0))
            .ToListAsync(cancellationToken);

        return new MyAllocationsDto(
            AllocationMonth.Format(month),
            AllocationMonth.CapacityHours(month),
            projects,
            projects.Sum(p => p.Hours),
            projects.Sum(p => p.LoggedHours));
    }
}
