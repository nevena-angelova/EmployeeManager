using EmployeeManager.Application.Employees.Management;

namespace EmployeeManager.Application.Projects;

/// <summary>
/// Projects that managers run with their direct reports. <c>currentEmployeeId</c> is always the signed-in employee:
/// only a project's owner can change it, and only its owner and members can see it.
/// </summary>
public interface IProjectService
{
    /// <summary>The projects the employee owns, sorted by name.</summary>
    Task<IReadOnlyList<ProjectSummaryDto>> ListManagedAsync(Guid currentEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>The projects the employee is a member of, sorted by name.</summary>
    Task<IReadOnlyList<ProjectSummaryDto>> ListMineAsync(Guid currentEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the project if the employee owns it or is on it, otherwise null.</summary>
    Task<ProjectDto?> GetAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default);

    Task<EmployeeResult<ProjectDto>> CreateAsync(
        SaveProjectRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the details and replaces the members. Not found unless the employee owns the project.</summary>
    Task<EmployeeResult<ProjectDto>> UpdateAsync(
        Guid id,
        SaveProjectRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the project. Not found unless the employee owns it.</summary>
    Task<EmployeeResult> DeleteAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default);
}
