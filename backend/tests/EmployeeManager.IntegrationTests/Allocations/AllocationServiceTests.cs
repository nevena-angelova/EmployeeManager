using EmployeeManager.Application.Allocations;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.IntegrationTests.Allocations;

public sealed class AllocationServiceTests : IDisposable
{
    private static readonly DateOnly November = new(2026, 11, 1);
    private static readonly DateOnly October = new(2026, 10, 1);

    private readonly TestDatabase database = new();
    private readonly Employee manager = NewEmployee("Mia", "Manager");
    private readonly Employee report = NewEmployee("Rachel", "Report");
    private readonly Employee otherReport = NewEmployee("Oscar", "Report");
    private readonly Employee otherManager = NewEmployee("Noah", "Other");
    private readonly Employee stranger = NewEmployee("Sam", "Stranger");
    private readonly Project alpha;
    private readonly Project beta;
    private readonly Project gamma;

    public AllocationServiceTests()
    {
        report.ManagerId = manager.Id;
        otherReport.ManagerId = manager.Id;
        stranger.ManagerId = otherManager.Id;

        alpha = NewProject("Alpha", manager, report, otherReport);
        beta = NewProject("Beta", manager, report);
        // Rachel stayed on her previous manager's project.
        gamma = NewProject("Gamma", otherManager, report, stranger);

        using var context = database.CreateContext();
        context.Employees.AddRange(manager, report, otherReport, otherManager, stranger);
        context.Projects.AddRange(alpha, beta, gamma);
        context.SaveChanges();
    }

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task GetTeamAsync_ReturnsReportsProjectsAndHours()
    {
        await AddHoursAsync((report, alpha, 40), (report, gamma, 20), (otherReport, alpha, 10));

        await using var context = database.CreateContext();
        var team = await NewService(context).GetTeamAsync(November, manager.Id);

        Assert.Equal("2026-11", team.Month);
        Assert.Equal(168, team.CapacityHours);
        Assert.True(team.IsEditable);
        Assert.Equal(["Alpha", "Beta"], team.Projects.Select(p => p.Name));
        Assert.Equal([otherReport.Id, report.Id], team.Members.Select(m => m.EmployeeId));

        var rachel = team.Members[1];
        Assert.Equal([alpha.Id, beta.Id], rachel.ProjectIds);
        Assert.Equal([new ProjectHoursDto(alpha.Id, 40)], rachel.Hours);
        Assert.Equal(20, rachel.OtherHours);
    }

    [Fact]
    public async Task GetTeamAsync_ForAPastMonth_IsNotEditable()
    {
        await using var context = database.CreateContext();
        Assert.False((await NewService(context).GetTeamAsync(October, manager.Id)).IsEditable);
    }

    [Fact]
    public async Task SaveTeamAsync_AddsUpdatesAndRemovesHours()
    {
        await AddHoursAsync((report, alpha, 40), (report, beta, 30), (otherReport, alpha, 10));

        var result = await SaveAsync(November, (report, alpha, 50.5m), (report, beta, 0), (otherReport, alpha, 0));

        Assert.Equal(EmployeeResultStatus.Success, result.Status);
        await using var context = database.CreateContext();
        var stored = await context.HourAllocations
            .Where(a => a.Project!.OwnerId == manager.Id)
            .Select(a => new { a.EmployeeId, a.ProjectId, a.Hours })
            .ToListAsync();
        var only = Assert.Single(stored);
        Assert.Equal((report.Id, alpha.Id, 50.5m), (only.EmployeeId, only.ProjectId, only.Hours));
        Assert.Equal([new ProjectHoursDto(alpha.Id, 50.5m)], result.Value!.Members.Single(m => m.EmployeeId == report.Id).Hours);
    }

    [Fact]
    public async Task SaveTeamAsync_LeavesOtherManagersHoursAlone()
    {
        await AddHoursAsync((report, gamma, 20));

        await SaveAsync(November, (report, alpha, 10));

        await using var context = database.CreateContext();
        Assert.True(await context.HourAllocations.AnyAsync(a => a.ProjectId == gamma.Id && a.Hours == 20));
    }

    [Fact]
    public async Task SaveTeamAsync_ForAPastMonth_ReturnsConflict()
    {
        var result = await SaveAsync(October, (report, alpha, 10));

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task SaveTeamAsync_OverCapacityWithOtherHours_ReturnsErrorForThatEmployee()
    {
        await AddHoursAsync((report, gamma, 100));

        var result = await SaveAsync(November, (report, alpha, 60), (report, beta, 10), (otherReport, alpha, 100));

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal($"allocations.{report.Id}", error.Key);
        Assert.Contains("170h", error.Value[0]);
    }

    [Fact]
    public async Task SaveTeamAsync_OnAProjectTheyAreNotOn_ReturnsError()
    {
        var result = await SaveAsync(November, (otherReport, beta, 10));

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        Assert.Contains($"allocations.{otherReport.Id}", result.Errors.Keys);
    }

    [Fact]
    public async Task SaveTeamAsync_ForSomeoneElsesReport_ReturnsError()
    {
        var result = await SaveAsync(November, (stranger, alpha, 10));

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        Assert.Contains("allocations", result.Errors.Keys);
    }

    [Fact]
    public async Task SaveTeamAsync_WhenAnotherSaveAddsTheSameHours_ReturnsConflict()
    {
        await using var context = database.CreateContext(new BeforeFirstSave(() => AddHoursAsync((report, alpha, 5))));

        var result = await NewService(context).SaveTeamAsync(
            November,
            new SaveTeamAllocationsRequest([new SaveAllocationItem(report.Id, alpha.Id, 10)]),
            manager.Id);

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task GetMineAsync_ReturnsHoursOnEveryProject()
    {
        await AddHoursAsync((report, alpha, 40), (report, gamma, 20));
        await AddHoursAsync(October, (report, beta, 99));

        await using var context = database.CreateContext();
        var mine = await NewService(context).GetMineAsync(November, report.Id);

        Assert.Equal(168, mine.CapacityHours);
        Assert.Equal(60, mine.TotalHours);
        Assert.Equal(
            [("Alpha", "Mia Manager", 40m), ("Beta", "Mia Manager", 0m), ("Gamma", "Noah Other", 20m)],
            mine.Projects.Select(p => (p.ProjectName, p.OwnerName, p.Hours)));
    }

    [Fact]
    public async Task GetMineAsync_ReturnsTheMonthsLoggedHours()
    {
        var delta = NewProject("Delta", otherManager);
        await using (var setup = database.CreateContext())
        {
            setup.Projects.Add(delta);
            await setup.SaveChangesAsync();
        }

        await AddHoursAsync((report, alpha, 40));
        await AddEntriesAsync(
            (report, alpha, new DateOnly(2026, 11, 2), 6),
            (report, alpha, new DateOnly(2026, 11, 30), 2.5m),
            (report, alpha, new DateOnly(2026, 10, 30), 8),
            // Not on Delta, but logged time on it this month.
            (report, delta, new DateOnly(2026, 11, 3), 1));

        await using var context = database.CreateContext();
        var mine = await NewService(context).GetMineAsync(November, report.Id);

        Assert.Equal(
            [("Alpha", 40m, 8.5m), ("Beta", 0m, 0m), ("Delta", 0m, 1m), ("Gamma", 0m, 0m)],
            mine.Projects.Select(p => (p.ProjectName, p.Hours, p.LoggedHours)));
        Assert.Equal(40, mine.TotalHours);
        Assert.Equal(9.5m, mine.TotalLoggedHours);
    }

    [Fact]
    public async Task GetTeamAsync_ReturnsLoggedHours()
    {
        await AddEntriesAsync(
            (report, beta, new DateOnly(2026, 11, 2), 4),
            (report, alpha, new DateOnly(2026, 11, 2), 3),
            (report, alpha, new DateOnly(2026, 11, 3), 3),
            (report, gamma, new DateOnly(2026, 11, 4), 2),
            (report, alpha, new DateOnly(2026, 12, 1), 7),
            (stranger, gamma, new DateOnly(2026, 11, 4), 5));

        await using var context = database.CreateContext();
        var team = await NewService(context).GetTeamAsync(November, manager.Id);

        var rachel = team.Members.Single(m => m.EmployeeId == report.Id);
        Assert.Equal([new ProjectHoursDto(alpha.Id, 6), new ProjectHoursDto(beta.Id, 4)], rachel.Logged);
        Assert.Equal(2, rachel.OtherLoggedHours);

        var oscar = team.Members.Single(m => m.EmployeeId == otherReport.Id);
        Assert.Empty(oscar.Logged);
        Assert.Equal(0, oscar.OtherLoggedHours);
    }

    [Fact]
    public async Task GetTeamStatisticsAsync_SumsTheYearByMonthProjectAndReport()
    {
        await AddHoursAsync((report, alpha, 40), (report, gamma, 20), (otherReport, alpha, 10));
        await AddHoursAsync(October, (report, beta, 30));
        await AddHoursAsync(new DateOnly(2025, 11, 1), (report, alpha, 99));
        await AddHoursAsync((stranger, gamma, 50));
        await AddEntriesAsync(
            (report, alpha, new DateOnly(2026, 11, 2), 6),
            (report, alpha, new DateOnly(2026, 11, 3), 2.5m),
            (report, gamma, new DateOnly(2026, 11, 3), 3),
            (report, beta, new DateOnly(2026, 10, 30), 8),
            (report, alpha, new DateOnly(2025, 12, 31), 7));

        await using var context = database.CreateContext();
        var statistics = await NewService(context).GetTeamStatisticsAsync(2026, manager.Id);

        Assert.Equal(2026, statistics.Year);
        Assert.Equal(12, statistics.Months.Count);
        Assert.Equal(
            new MonthStatisticsDto("2026-11", 168, 50, 8.5m, 20, 3),
            statistics.Months[10]);
        Assert.Equal(new MonthStatisticsDto("2026-10", 176, 30, 8, 0, 0), statistics.Months[9]);
        Assert.Equal(["Alpha", "Beta"], statistics.Projects.Select(p => p.Name));
        Assert.Equal((50m, 8.5m), (statistics.Projects[0].TotalPlanned, statistics.Projects[0].TotalLogged));
        Assert.Equal([otherReport.Id, report.Id], statistics.Members.Select(m => m.EmployeeId));
        Assert.Equal((70m, 16.5m), (statistics.Members[1].TotalPlanned, statistics.Members[1].TotalLogged));
        Assert.Equal((80m, 16.5m), (statistics.TotalPlanned, statistics.TotalLogged));
    }

    private async Task AddEntriesAsync(params (Employee Employee, Project Project, DateOnly Date, decimal Hours)[] entries)
    {
        await using var context = database.CreateContext();
        context.TimeEntries.AddRange(entries.Select(e => new TimeEntry
        {
            EmployeeId = e.Employee.Id,
            ProjectId = e.Project.Id,
            Date = e.Date,
            Hours = e.Hours,
        }));
        await context.SaveChangesAsync();
    }

    private Task AddHoursAsync(params (Employee Employee, Project Project, decimal Hours)[] hours) =>
        AddHoursAsync(November, hours);

    private async Task AddHoursAsync(DateOnly month, params (Employee Employee, Project Project, decimal Hours)[] hours)
    {
        await using var context = database.CreateContext();
        context.HourAllocations.AddRange(hours.Select(h => new HourAllocation
        {
            EmployeeId = h.Employee.Id,
            ProjectId = h.Project.Id,
            Month = month,
            Hours = h.Hours,
        }));
        await context.SaveChangesAsync();
    }

    private async Task<EmployeeResult<TeamAllocationsDto>> SaveAsync(
        DateOnly month,
        params (Employee Employee, Project Project, decimal Hours)[] hours)
    {
        await using var context = database.CreateContext();
        return await NewService(context).SaveTeamAsync(
            month,
            new SaveTeamAllocationsRequest(
                hours.Select(h => new SaveAllocationItem(h.Employee.Id, h.Project.Id, h.Hours)).ToList()),
            manager.Id);
    }

    private static AllocationService NewService(Infrastructure.Persistence.EmployeeManagerDbContext context) =>
        new(context, FixedTimeProvider.MidNovember2026);

    private static Project NewProject(string name, Employee owner, params Employee[] members) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OwnerId = owner.Id,
        Members = members.Select(m => new ProjectMember { EmployeeId = m.Id }).ToList(),
    };

    private static Employee NewEmployee(string firstName, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName = lastName,
        Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
        JobTitle = "Engineer",
    };
}
