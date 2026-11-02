namespace EmployeeManager.Application.Allocations;

/// <summary>A manager's allocations for one month: their direct reports' hours on the manager's projects.</summary>
/// <param name="Month">The month, as <c>yyyy-MM</c>.</param>
/// <param name="CapacityHours">The hours each employee has available in the month.</param>
/// <param name="IsEditable">False for past months, which can't be changed.</param>
/// <param name="Projects">The manager's projects, sorted by name.</param>
/// <param name="Members">The manager's direct reports, sorted by name.</param>
public sealed record TeamAllocationsDto(
    string Month,
    decimal CapacityHours,
    bool IsEditable,
    IReadOnlyList<AllocationProjectDto> Projects,
    IReadOnlyList<TeamMemberAllocationDto> Members);

public sealed record AllocationProjectDto(Guid Id, string Name);

/// <param name="ProjectIds">
/// The manager's projects the employee can be given hours on: the ones they're on, plus any they already have hours
/// on in this month.
/// </param>
/// <param name="Hours">Their hours on the manager's projects; projects without hours are left out.</param>
/// <param name="OtherHours">Their hours on other managers' projects, which count towards the month's capacity.</param>
public sealed record TeamMemberAllocationDto(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string JobTitle,
    IReadOnlyList<Guid> ProjectIds,
    IReadOnlyList<ProjectHoursDto> Hours,
    decimal OtherHours);

public sealed record ProjectHoursDto(Guid ProjectId, decimal Hours);

/// <summary>The signed-in employee's hours for one month.</summary>
/// <param name="Projects">The projects they're on or have hours on in the month, sorted by name.</param>
public sealed record MyAllocationsDto(
    string Month,
    decimal CapacityHours,
    IReadOnlyList<MyProjectHoursDto> Projects,
    decimal TotalHours);

public sealed record MyProjectHoursDto(Guid ProjectId, string ProjectName, string OwnerName, decimal Hours);

/// <summary>
/// Replaces a month's allocations for the manager's direct reports on the manager's projects. Anything left out, or
/// given zero hours, is removed.
/// </summary>
public sealed record SaveTeamAllocationsRequest(IReadOnlyList<SaveAllocationItem>? Allocations);

public sealed record SaveAllocationItem(Guid EmployeeId, Guid ProjectId, decimal Hours);
