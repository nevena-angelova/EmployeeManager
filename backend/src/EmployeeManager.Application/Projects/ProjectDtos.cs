using System.ComponentModel.DataAnnotations;

namespace EmployeeManager.Application.Projects;

/// <summary>A project in a list: its members are only counted.</summary>
public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    int MemberCount);

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    IReadOnlyList<ProjectMemberDto> Members);

/// <param name="IsDirectReport">
/// Whether the member still reports to the project's owner. Members who were moved to another manager stay on the
/// project until the owner removes them.
/// </param>
public sealed record ProjectMemberDto(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string JobTitle,
    bool IsDirectReport);

/// <summary>Creates a project, or replaces an existing one's details and members.</summary>
public sealed record SaveProjectRequest(
    [Required, MaxLength(150)] string Name,
    [MaxLength(2000)] string? Description,
    IReadOnlyList<Guid>? MemberIds);
