using EmployeeManager.Application.Projects;

namespace EmployeeManager.UnitTests.Projects;

public class ProjectMemberRulesTests
{
    private static readonly Guid Report = Guid.NewGuid();
    private static readonly Guid OtherReport = Guid.NewGuid();
    private static readonly Guid FormerReport = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static readonly HashSet<Guid> DirectReports = [Report, OtherReport];
    private static readonly HashSet<Guid> NoMembers = [];

    [Fact]
    public void Validate_WithNoMembers_IsValid() =>
        Assert.Null(ProjectMemberRules.Validate([], DirectReports, NoMembers));

    [Fact]
    public void Validate_WithDirectReports_IsValid() =>
        Assert.Null(ProjectMemberRules.Validate([Report, OtherReport], DirectReports, NoMembers));

    [Fact]
    public void Validate_WithDuplicates_IsValid() =>
        Assert.Null(ProjectMemberRules.Validate([Report, Report], DirectReports, NoMembers));

    [Fact]
    public void Validate_WithSomeoneElsesReport_ReturnsError() =>
        Assert.Contains("direct reports", ProjectMemberRules.Validate([Report, Stranger], DirectReports, NoMembers));

    [Fact]
    public void Validate_KeepingFormerReportAlreadyOnTheProject_IsValid() =>
        Assert.Null(ProjectMemberRules.Validate([Report, FormerReport], DirectReports, new HashSet<Guid> { FormerReport }));

    [Fact]
    public void Validate_AddingFormerReportNotOnTheProject_ReturnsError() =>
        Assert.NotNull(ProjectMemberRules.Validate([FormerReport], DirectReports, new HashSet<Guid> { Report }));
}
