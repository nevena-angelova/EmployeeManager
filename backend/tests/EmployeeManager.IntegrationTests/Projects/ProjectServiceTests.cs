using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Application.Projects;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.IntegrationTests.Projects;

public sealed class ProjectServiceTests : IDisposable
{
    private readonly TestDatabase database = new();
    private readonly Employee owner = NewEmployee("Olivia", "Owner");
    private readonly Employee report = NewEmployee("Rachel", "Report");
    private readonly Employee otherReport = NewEmployee("Oscar", "Report");

    public ProjectServiceTests()
    {
        report.ManagerId = owner.Id;
        otherReport.ManagerId = owner.Id;

        using var context = database.CreateContext();
        context.Employees.AddRange(owner, report, otherReport);
        context.SaveChanges();
    }

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task UpdateAsync_ChangingOnlyMembers_StampsUpdatedAt()
    {
        var project = await CreateProjectAsync([report.Id]);

        var result = await UpdateAsync(project.Id, [report.Id, otherReport.Id]);

        Assert.Equal(EmployeeResultStatus.Success, result.Status);
        Assert.Equal(2, result.Value!.Members.Count);
        Assert.NotNull(await GetUpdatedAtAsync(project.Id));
    }

    [Fact]
    public async Task UpdateAsync_ChangingNothing_LeavesUpdatedAt()
    {
        var project = await CreateProjectAsync([report.Id]);

        await UpdateAsync(project.Id, [report.Id]);

        Assert.Null(await GetUpdatedAtAsync(project.Id));
    }

    [Fact]
    public async Task UpdateAsync_WhenAnotherSaveAddsTheSameMember_ReturnsConflict()
    {
        var project = await CreateProjectAsync([]);

        var result = await UpdateAsync(project.Id, [report.Id], beforeSave: async () =>
        {
            await using var other = database.CreateContext();
            other.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, EmployeeId = report.Id });
            await other.SaveChangesAsync();
        });

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenANewMemberIsDeletedFirst_ReturnsConflict()
    {
        var project = await CreateProjectAsync([]);

        var result = await UpdateAsync(project.Id, [report.Id], beforeSave: async () =>
        {
            await using var other = database.CreateContext();
            await other.Employees.Where(e => e.Id == report.Id).ExecuteDeleteAsync();
        });

        Assert.Equal(EmployeeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenAnotherSaveTakesTheName_ReturnsNameError()
    {
        var project = await CreateProjectAsync([]);

        var result = await UpdateAsync(project.Id, [], name: "Billing", beforeSave: async () =>
        {
            await using var other = database.CreateContext();
            other.Projects.Add(new Project { Name = "BILLING", OwnerId = owner.Id });
            await other.SaveChangesAsync();
        });

        Assert.Equal(EmployeeResultStatus.Invalid, result.Status);
        Assert.Contains("name", result.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_RemovingAMember_RemovesTheirHoursFromThisMonthOn()
    {
        var project = await CreateProjectAsync([report.Id, otherReport.Id]);
        await using (var context = database.CreateContext())
        {
            DateOnly[] months = [new(2026, 10, 1), new(2026, 11, 1), new(2026, 12, 1)];
            context.HourAllocations.AddRange(months.Select(month =>
                new HourAllocation { ProjectId = project.Id, EmployeeId = report.Id, Month = month, Hours = 10 }));
            context.HourAllocations.Add(
                new HourAllocation { ProjectId = project.Id, EmployeeId = otherReport.Id, Month = months[1], Hours = 10 });
            await context.SaveChangesAsync();
        }

        await UpdateAsync(project.Id, [otherReport.Id]);

        await using var check = database.CreateContext();
        var left = await check.HourAllocations
            .OrderBy(a => a.Month)
            .Select(a => new { a.EmployeeId, a.Month })
            .ToListAsync();
        Assert.Collection(
            left,
            a => Assert.Equal((report.Id, new DateOnly(2026, 10, 1)), (a.EmployeeId, a.Month)),
            a => Assert.Equal((otherReport.Id, new DateOnly(2026, 11, 1)), (a.EmployeeId, a.Month)));
    }

    private async Task<ProjectDto> CreateProjectAsync(IReadOnlyList<Guid> memberIds)
    {
        await using var context = database.CreateContext();
        var result = await new ProjectService(context, FixedTimeProvider.MidNovember2026).CreateAsync(
            new SaveProjectRequest("Payroll", null, memberIds),
            owner.Id);
        Assert.Equal(EmployeeResultStatus.Success, result.Status);
        return result.Value!;
    }

    private async Task<EmployeeResult<ProjectDto>> UpdateAsync(
        Guid id,
        IReadOnlyList<Guid> memberIds,
        string name = "Payroll",
        Func<Task>? beforeSave = null)
    {
        await using var context = beforeSave is null
            ? database.CreateContext()
            : database.CreateContext(new BeforeFirstSave(beforeSave));
        return await new ProjectService(context, FixedTimeProvider.MidNovember2026).UpdateAsync(id, new SaveProjectRequest(name, null, memberIds), owner.Id);
    }

    private async Task<DateTime?> GetUpdatedAtAsync(Guid projectId)
    {
        await using var context = database.CreateContext();
        return await context.Projects.Where(p => p.Id == projectId).Select(p => p.UpdatedAt).SingleAsync();
    }

    private static Employee NewEmployee(string firstName, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName = lastName,
        Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
        JobTitle = "Engineer",
    };
}
