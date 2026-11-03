namespace EmployeeManager.Application.Allocations;

/// <summary>A direct report, as a row of the statistics.</summary>
public sealed record StatisticsMember(Guid EmployeeId, string FirstName, string LastName, string JobTitle);

/// <summary>
/// A report's hours on one project in one month: allocated, or the sum of their daily time entries.
/// <paramref name="IsOwn"/> is whether the project is the manager's.
/// </summary>
public sealed record StatisticsHours(Guid EmployeeId, Guid ProjectId, DateOnly Month, decimal Hours, bool IsOwn);

/// <summary>Sums a year of planned and logged hours by month, project and direct report.</summary>
public static class AllocationStatisticsBuilder
{
    /// <param name="projects">The manager's projects, in the order to return them.</param>
    /// <param name="members">The manager's direct reports, in the order to return them.</param>
    /// <param name="planned">Allocations in the year. Rows for anyone not in <paramref name="members"/> are ignored.</param>
    /// <param name="logged">Logged hours in the year, at most one row per employee, project and month.</param>
    public static TeamStatisticsDto Build(
        int year,
        IReadOnlyList<AllocationProjectDto> projects,
        IReadOnlyList<StatisticsMember> members,
        IReadOnlyCollection<StatisticsHours> planned,
        IReadOnlyCollection<StatisticsHours> logged)
    {
        var months = Enumerable.Range(1, 12).Select(m => new DateOnly(year, m, 1)).ToList();
        var memberIds = members.Select(m => m.EmployeeId).ToHashSet();
        var ownProjectIds = projects.Select(p => p.Id).ToHashSet();
        List<StatisticsHours> team(IReadOnlyCollection<StatisticsHours> rows) =>
            rows.Where(r => memberIds.Contains(r.EmployeeId) && r.Month.Year == year).ToList();

        var teamPlanned = team(planned);
        var teamLogged = team(logged);
        var ownPlanned = teamPlanned.Where(r => r.IsOwn && ownProjectIds.Contains(r.ProjectId)).ToList();
        var ownLogged = teamLogged.Where(r => r.IsOwn && ownProjectIds.Contains(r.ProjectId)).ToList();

        List<MonthHoursDto> ByMonth(Func<StatisticsHours, bool> include) =>
            months
                .Select(month => new MonthHoursDto(
                    AllocationMonth.Format(month),
                    ownPlanned.Where(r => r.Month == month && include(r)).Sum(r => r.Hours),
                    ownLogged.Where(r => r.Month == month && include(r)).Sum(r => r.Hours)))
                .ToList();

        return new TeamStatisticsDto(
            year,
            months
                .Select(month => new MonthStatisticsDto(
                    AllocationMonth.Format(month),
                    AllocationMonth.CapacityHours(month),
                    ownPlanned.Where(r => r.Month == month).Sum(r => r.Hours),
                    ownLogged.Where(r => r.Month == month).Sum(r => r.Hours),
                    teamPlanned.Where(r => r.Month == month && !r.IsOwn).Sum(r => r.Hours),
                    teamLogged.Where(r => r.Month == month && !r.IsOwn).Sum(r => r.Hours)))
                .ToList(),
            projects
                .Select(p =>
                {
                    var byMonth = ByMonth(r => r.ProjectId == p.Id);
                    return new ProjectStatisticsDto(
                        p.Id,
                        p.Name,
                        byMonth,
                        byMonth.Sum(m => m.Planned),
                        byMonth.Sum(m => m.Logged));
                })
                .ToList(),
            members
                .Select(m =>
                {
                    var byMonth = ByMonth(r => r.EmployeeId == m.EmployeeId);
                    return new MemberStatisticsDto(
                        m.EmployeeId,
                        m.FirstName,
                        m.LastName,
                        m.JobTitle,
                        byMonth,
                        byMonth.Sum(h => h.Planned),
                        byMonth.Sum(h => h.Logged));
                })
                .ToList(),
            ownPlanned.Sum(r => r.Hours),
            ownLogged.Sum(r => r.Hours));
    }
}
