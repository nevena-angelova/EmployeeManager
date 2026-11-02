namespace EmployeeManager.Application.Projects;

public static class ProjectMemberRules
{
    /// <summary>
    /// Returns why <paramref name="memberIds"/> can't be a project's members, or null if they can. Every member must be
    /// one of the owner's <paramref name="directReports"/>, except people already on the project
    /// (<paramref name="currentMemberIds"/>), who keep their place after moving to another manager.
    /// </summary>
    public static string? Validate(
        IEnumerable<Guid> memberIds,
        IReadOnlySet<Guid> directReports,
        IReadOnlySet<Guid> currentMemberIds) =>
        memberIds.All(id => directReports.Contains(id) || currentMemberIds.Contains(id))
            ? null
            : "Only your direct reports can be added to a project.";
}
