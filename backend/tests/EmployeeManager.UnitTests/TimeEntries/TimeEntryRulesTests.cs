using EmployeeManager.Application.TimeEntries;

namespace EmployeeManager.UnitTests.TimeEntries;

public class TimeEntryRulesTests
{
    private static readonly Guid Alpha = Guid.NewGuid();
    private static readonly Guid Beta = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();
    private static readonly HashSet<Guid> OnAlphaAndBeta = [Alpha, Beta];

    [Fact]
    public void Validate_UpToADay_IsValid() =>
        Assert.Null(TimeEntryRules.Validate([new(Alpha, 20), new(Beta, 4)], OnAlphaAndBeta));

    [Fact]
    public void Validate_WithNoEntries_IsValid() =>
        Assert.Null(TimeEntryRules.Validate([], OnAlphaAndBeta));

    [Fact]
    public void Validate_OverADay_ReturnsError() =>
        Assert.Equal(
            "That's 24.5h; a day has at most 24h.",
            TimeEntryRules.Validate([new(Alpha, 20), new(Beta, 4.5m)], OnAlphaAndBeta));

    [Fact]
    public void Validate_WithTheSameProjectTwice_ReturnsError() =>
        Assert.Contains("once", TimeEntryRules.Validate([new(Alpha, 2), new(Alpha, 3)], OnAlphaAndBeta));

    [Fact]
    public void Validate_NegativeHours_ReturnsError() =>
        Assert.Contains("negative", TimeEntryRules.Validate([new(Alpha, -1)], OnAlphaAndBeta));

    [Fact]
    public void Validate_NotInHalfHours_ReturnsError() =>
        Assert.Contains("half an hour", TimeEntryRules.Validate([new(Alpha, 1.25m)], OnAlphaAndBeta));

    [Fact]
    public void Validate_HoursOnAProjectTheyAreNotOn_ReturnsError() =>
        Assert.Contains("projects you're on", TimeEntryRules.Validate([new(Other, 1)], OnAlphaAndBeta));

    [Fact]
    public void Validate_ZeroHoursOnAProjectTheyAreNotOn_IsValid() =>
        Assert.Null(TimeEntryRules.Validate([new(Other, 0)], OnAlphaAndBeta));
}
