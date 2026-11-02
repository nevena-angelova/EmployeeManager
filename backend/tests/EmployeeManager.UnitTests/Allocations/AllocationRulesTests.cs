using EmployeeManager.Application.Allocations;

namespace EmployeeManager.UnitTests.Allocations;

public class AllocationRulesTests
{
    private static readonly DateOnly November = new(2026, 11, 1); // 168h
    private static readonly Guid Report = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();
    private static readonly Guid Alpha = Guid.NewGuid();
    private static readonly Guid Beta = Guid.NewGuid();
    private static readonly HashSet<Guid> OnAlphaAndBeta = [Alpha, Beta];

    [Fact]
    public void ValidateRequest_ForDirectReports_IsValid() =>
        Assert.Null(AllocationRules.ValidateRequest(
            [new SaveAllocationItem(Report, Alpha, 10), new SaveAllocationItem(Report, Beta, 10)],
            new HashSet<Guid> { Report }));

    [Fact]
    public void ValidateRequest_ForSomeoneElse_ReturnsError() =>
        Assert.Contains("direct reports", AllocationRules.ValidateRequest(
            [new SaveAllocationItem(Stranger, Alpha, 10)],
            new HashSet<Guid> { Report }));

    [Fact]
    public void ValidateRequest_WithTheSameProjectTwice_ReturnsError() =>
        Assert.NotNull(AllocationRules.ValidateRequest(
            [new SaveAllocationItem(Report, Alpha, 10), new SaveAllocationItem(Report, Alpha, 5)],
            new HashSet<Guid> { Report }));

    [Fact]
    public void ValidateEmployee_UpToCapacity_IsValid() =>
        Assert.Null(Validate([new(Alpha, 100), new(Beta, 48)], otherHours: 20));

    [Fact]
    public void ValidateEmployee_OverCapacityWithOtherHours_ReturnsError() =>
        Assert.Equal(
            "Rachel Report would have 168.5h; November 2026 has 168h available.",
            Validate([new(Alpha, 100), new(Beta, 48.5m)], otherHours: 20));

    [Fact]
    public void ValidateEmployee_WithNegativeHours_ReturnsError() =>
        Assert.Contains("negative", Validate([new(Alpha, -1)]));

    [Fact]
    public void ValidateEmployee_WithQuarterHours_ReturnsError() =>
        Assert.Contains("half an hour", Validate([new(Alpha, 10.25m)]));

    [Fact]
    public void ValidateEmployee_OnAProjectTheyAreNotOn_ReturnsError() =>
        Assert.Contains("isn't on", Validate([new(Guid.NewGuid(), 10)]));

    [Fact]
    public void ValidateEmployee_WithZeroHoursOnAProjectTheyAreNotOn_IsValid() =>
        Assert.Null(Validate([new(Guid.NewGuid(), 0)]));

    private static string? Validate(IReadOnlyCollection<ProjectHoursDto> hours, decimal otherHours = 0) =>
        AllocationRules.ValidateEmployee("Rachel Report", hours, OnAlphaAndBeta, otherHours, November);
}
