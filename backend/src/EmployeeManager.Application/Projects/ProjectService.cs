using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Application.Projects;

public class ProjectService(IApplicationDbContext db) : IProjectService
{
    private const string DuplicateNameError = "You already have a project with this name.";

    private const string ConcurrentChangeError =
        "The project or its members changed while you were saving. Reload the project and try again.";

    public Task<IReadOnlyList<ProjectSummaryDto>> ListManagedAsync(
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default) =>
        ListAsync(db.Projects.Where(p => p.OwnerId == currentEmployeeId), cancellationToken);

    public Task<IReadOnlyList<ProjectSummaryDto>> ListMineAsync(
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default) =>
        ListAsync(db.Projects.Where(p => p.Members.Any(m => m.EmployeeId == currentEmployeeId)), cancellationToken);

    public Task<ProjectDto?> GetAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default) =>
        db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id
                && (p.OwnerId == currentEmployeeId || p.Members.Any(m => m.EmployeeId == currentEmployeeId)))
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
                p.Owner!.FirstName + " " + p.Owner.LastName,
                p.Members
                    .OrderBy(m => m.Employee!.FirstName)
                    .ThenBy(m => m.Employee!.LastName)
                    .Select(m => new ProjectMemberDto(
                        m.EmployeeId,
                        m.Employee!.FirstName,
                        m.Employee.LastName,
                        m.Employee.JobTitle,
                        m.Employee.ManagerId == p.OwnerId))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<EmployeeResult<ProjectDto>> CreateAsync(
        SaveProjectRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var (name, description, memberIds) = Normalise(request);
        var invalid = await ValidateAsync(null, name, memberIds, new HashSet<Guid>(), currentEmployeeId, cancellationToken);
        if (invalid is not null)
        {
            return EmployeeResult<ProjectDto>.From(invalid);
        }

        var project = new Project
        {
            Name = name,
            Description = description,
            OwnerId = currentEmployeeId,
            Members = memberIds.Select(id => new ProjectMember { EmployeeId = id }).ToList(),
        };
        db.Projects.Add(project);
        if (await SaveAsync(null, name, currentEmployeeId, cancellationToken) is { } violation)
        {
            return EmployeeResult<ProjectDto>.From(violation);
        }

        return EmployeeResult<ProjectDto>.Success((await GetAsync(project.Id, currentEmployeeId, cancellationToken))!);
    }

    public async Task<EmployeeResult<ProjectDto>> UpdateAsync(
        Guid id,
        SaveProjectRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var project = await db.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == currentEmployeeId, cancellationToken);
        if (project is null)
        {
            return EmployeeResult<ProjectDto>.From(EmployeeResult.NotFound());
        }

        var (name, description, memberIds) = Normalise(request);
        var currentMemberIds = project.Members.Select(m => m.EmployeeId).ToHashSet();
        var invalid = await ValidateAsync(id, name, memberIds, currentMemberIds, currentEmployeeId, cancellationToken);
        if (invalid is not null)
        {
            return EmployeeResult<ProjectDto>.From(invalid);
        }

        project.Name = name;
        project.Description = description;

        foreach (var member in project.Members.Where(m => !memberIds.Contains(m.EmployeeId)).ToList())
        {
            db.ProjectMembers.Remove(member);
        }

        foreach (var employeeId in memberIds.Where(e => !currentMemberIds.Contains(e)))
        {
            db.ProjectMembers.Add(new ProjectMember { ProjectId = id, EmployeeId = employeeId });
        }

        if (await SaveAsync(id, name, currentEmployeeId, cancellationToken) is { } violation)
        {
            return EmployeeResult<ProjectDto>.From(violation);
        }

        return EmployeeResult<ProjectDto>.Success((await GetAsync(id, currentEmployeeId, cancellationToken))!);
    }

    public async Task<EmployeeResult> DeleteAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(
            p => p.Id == id && p.OwnerId == currentEmployeeId,
            cancellationToken);
        if (project is null)
        {
            return EmployeeResult.NotFound();
        }

        // Its members are removed by the cascade on project_members.project_id.
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return EmployeeResult.Success();
    }

    private static async Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(
        IQueryable<Project> projects,
        CancellationToken cancellationToken) =>
        await projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
                p.Owner!.FirstName + " " + p.Owner.LastName,
                p.Members.Count))
            .ToListAsync(cancellationToken);

    private static (string Name, string? Description, IReadOnlySet<Guid> MemberIds) Normalise(SaveProjectRequest request) =>
        (request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            (request.MemberIds ?? []).ToHashSet());

    /// <summary>Checks the rules data annotations can't: a name the owner hasn't used and members they may assign.</summary>
    private async Task<EmployeeResult?> ValidateAsync(
        Guid? id,
        string name,
        IReadOnlySet<Guid> memberIds,
        IReadOnlySet<Guid> currentMemberIds,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (name.Length == 0)
        {
            errors["name"] = ["The Name field is required."];
        }
        else if (await NameTakenAsync(id, name, ownerId, cancellationToken))
        {
            errors["name"] = [DuplicateNameError];
        }

        var directReports = (await db.Employees
                .Where(e => e.ManagerId == ownerId)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        if (ProjectMemberRules.Validate(memberIds, directReports, currentMemberIds) is { } memberError)
        {
            errors["memberIds"] = [memberError];
        }

        return errors.Count == 0 ? null : EmployeeResult.Invalid(errors);
    }

    // The name column's collation makes this comparison case-insensitive, like the unique index.
    private Task<bool> NameTakenAsync(Guid? id, string name, Guid ownerId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(p => p.Id != id && p.OwnerId == ownerId && p.Name == name, cancellationToken);

    /// <summary>
    /// Saves, or returns an error if something changed since <see cref="ValidateAsync"/>: the name error if a project
    /// with the same name was saved (which the unique index rejects), otherwise a conflict. The other failures are
    /// another save of the same project (a member added twice, or one already removed), or a member or the project
    /// being deleted. Application code can't see Postgres error codes, so it checks the name again.
    /// </summary>
    private async Task<EmployeeResult?> SaveAsync(Guid? id, string name, Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            return await NameTakenAsync(id, name, ownerId, cancellationToken)
                ? EmployeeResult.Invalid("name", DuplicateNameError)
                : EmployeeResult.Conflict(ConcurrentChangeError);
        }
    }
}
