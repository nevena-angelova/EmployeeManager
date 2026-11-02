using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Application.TimeEntries;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.IntegrationTests.TimeEntries;

public sealed class TimeEntryServiceTests : IDisposable
{
    // FixedTimeProvider.MidNovember2026 is Sunday 15 November; its week starts on Monday the 9th.
    private static readonly DateOnly Today = new(2026, 11, 15);
    private static readonly DateOnly Friday = new(2026, 11, 13);

    private readonly TestDatabase database = new();
    private readonly Employee manager = NewEmployee("Mia", "Manager");
    private readonly Employee employee = NewEmployee("Rachel", "Report");
    private readonly Employee colleague = NewEmployee("Oscar", "Report");
    private readonly Project alpha;
    private readonly Project beta;
    private readonly Project gamma;

    public TimeEntryServiceTests()
    {
        employee.ManagerId = manager.Id;
        colleague.ManagerId = manager.Id;

        alpha = NewProject("Alpha", manager, employee, colleague);
        beta = NewProject("Beta", manager, employee);
        // Rachel isn't on Gamma.
        gamma = NewProject("Gamma", manager, colleague);

        using var context = database.CreateContext();
        context.Employees.AddRange(manager, employee, colleague);
        context.Projects.AddRange(alpha, beta, gamma);
        context.SaveChanges();
    }

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task GetDayAsync_ReturnsHoursOnTheirProjectsAndTheWeek()
    {
        await AddHoursAsync(Friday, (employee, alpha, 6), (employee, beta, 1.5m), (colleague, alpha, 8));
        await AddHoursAsync(Friday.AddDays(-4), (employee, alpha, 7));
        await AddHoursAsync(Friday.AddDays(-5), (employee, alpha, 9)); // The Sunday before: last week.

        await using var context = database.CreateContext();
        var day = await NewService(context).GetDayAsync(Friday, employee.Id);

        Assert.Equal("2026-11-13", day.Date);
        Assert.True(day.IsEditable);
        Assert.Equal(24, day.MaxHours);
        Assert.Equal(7.5m, day.TotalHours);
        Assert.Equal(
            [("Alpha", "Mia Manager", 6m), ("Beta", "Mia Manager", 1.5m)],
            day.Projects.Select(p => (p.ProjectName, p.OwnerName, p.Hours)));
        Assert.Equal(
            [("2026-11-09", 7m), ("2026-11-10", 0m), ("2026-11-11", 0m), ("2026-11-12", 0m),
                ("2026-11-13", 7.5m), ("2026-11-14", 0m), ("2026-11-15", 0m)],
            day.Week.Select(d => (d.Date, d.Hours)));
    }

    [Fact]
    public async Task GetDayAsync_IncludesAProjectTheyLeftButLoggedHoursOn()
    {
        await AddHoursAsync(Friday, (employee, gamma, 2));

        await using var context = database.CreateContext();
        var day = await NewService(context).GetDayAsync(Friday, employee.Id);

        Assert.Equal(["Alpha", "Beta", "Gamma"], day.Projects.Select(p => p.ProjectName));
    }

    [Fact]
    public async Task GetDayAsync_ForTomorrow_IsNotEditable()
    {
        await using var context = database.CreateContext();
        Assert.False((await NewService(context).GetDayAsync(Today.AddDays(1), employee.Id)).IsEditable);
    }

    [Fact]
    public async Task SaveDayAsync_AddsUpdatesAndRemovesHours()
    {
        await AddHoursAsync(Friday, (employee, alpha, 6), (employee, beta, 2), (colleague, alpha, 8));

        var result = await SaveAsync(Friday, (alpha, 7.5m), (beta, 0));

        Assert.Equal(EmployeeResultStatus.Success, result.Status);
        Assert.Equal(7.5m, result.Value!.TotalHours);
        await using var context = database.CreateContext();
        var stored = await context.TimeEntries
            .Where(e => e.Date == Friday)
            .OrderBy(e => e.Hours)
            .Select(e => new { e.EmployeeId, e.ProjectId, e.Hours })
            .ToListAsync();
        Assert.Equal(
            [(employee.Id, alpha.Id, 7.5m), (colleague.Id, alpha.Id, 8m)],
            stored.Select(e => (e.EmployeeId, e.ProjectId, e.Hours)));
    }

    [Fact]
    public async Task SaveDayAsync_LeavesOtherDaysAlone()
    {
        await AddHoursAsync(Friday.AddDays(-1), (employee, alpha, 5));

        await SaveAsync(Friday, (alpha, 3));

        await using var context = database.CreateContext();
        Assert.True(await context.TimeEntries.AnyAsync(e => e.Date == Friday.AddDays(-1) && e.Hours == 5));
    }

    [Fact]
    public async Task SaveDayAsync_ForTomorrow_ReturnsConflict()
    {
        var result = await SaveAsync(Today.AddDays(1), (alpha, 3));

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task SaveDayAsync_OnAProjectTheyAreNotOn_ReturnsError()
    {
        var result = await SaveAsync(Friday, (gamma, 3));

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        Assert.Equal(["entries"], result.Errors.Keys);
    }

    [Fact]
    public async Task SaveDayAsync_OnAProjectTheyLeftButLoggedHoursOn_IsAllowed()
    {
        await AddHoursAsync(Friday, (employee, gamma, 2));

        var result = await SaveAsync(Friday, (gamma, 3));

        Assert.Equal(EmployeeResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task SaveDayAsync_OverADay_ReturnsError()
    {
        var result = await SaveAsync(Friday, (alpha, 20), (beta, 5));

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        Assert.Contains("25h", result.Errors["entries"][0]);
    }

    [Fact]
    public async Task SaveDayAsync_WhenAnotherSaveAddsTheSameHours_ReturnsConflict()
    {
        await using var context = database.CreateContext(
            new BeforeFirstSave(() => AddHoursAsync(Friday, (employee, alpha, 5))));

        var result = await NewService(context).SaveDayAsync(
            Friday,
            new SaveDayTimeEntriesRequest([new SaveTimeEntryItem(alpha.Id, 3)]),
            employee.Id);

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    private async Task AddHoursAsync(DateOnly date, params (Employee Employee, Project Project, decimal Hours)[] hours)
    {
        await using var context = database.CreateContext();
        context.TimeEntries.AddRange(hours.Select(h => new TimeEntry
        {
            EmployeeId = h.Employee.Id,
            ProjectId = h.Project.Id,
            Date = date,
            Hours = h.Hours,
        }));
        await context.SaveChangesAsync();
    }

    private async Task<EmployeeResult<DayTimeEntriesDto>> SaveAsync(
        DateOnly date,
        params (Project Project, decimal Hours)[] hours)
    {
        await using var context = database.CreateContext();
        return await NewService(context).SaveDayAsync(
            date,
            new SaveDayTimeEntriesRequest(hours.Select(h => new SaveTimeEntryItem(h.Project.Id, h.Hours)).ToList()),
            employee.Id);
    }

    private static TimeEntryService NewService(Infrastructure.Persistence.EmployeeManagerDbContext context) =>
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
