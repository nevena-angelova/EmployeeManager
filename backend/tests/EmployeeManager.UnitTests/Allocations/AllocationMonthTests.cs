using EmployeeManager.Application.Allocations;

namespace EmployeeManager.UnitTests.Allocations;

public class AllocationMonthTests
{
    [Fact]
    public void TryParse_WithYearAndMonth_ReturnsTheFirstDay()
    {
        Assert.True(AllocationMonth.TryParse("2026-11", out var month));
        Assert.Equal(new DateOnly(2026, 11, 1), month);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-1")]
    [InlineData("2026-11-01")]
    [InlineData("November")]
    public void TryParse_WithAnythingElse_Fails(string? value) => Assert.False(AllocationMonth.TryParse(value, out _));

    [Fact]
    public void Format_ReturnsYearAndMonth() => Assert.Equal("2026-03", AllocationMonth.Format(new DateOnly(2026, 3, 1)));

    [Theory]
    [InlineData(2026, 2, 160)] // 20 weekdays
    [InlineData(2026, 11, 168)] // 21 weekdays
    [InlineData(2026, 12, 184)] // 23 weekdays
    public void CapacityHours_IsEightHoursPerWeekday(int year, int month, decimal expected) =>
        Assert.Equal(expected, AllocationMonth.CapacityHours(new DateOnly(year, month, 1)));

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    [InlineData(12, true)]
    public void IsEditable_OnlyForTheCurrentAndLaterMonths(int month, bool expected) =>
        Assert.Equal(expected, AllocationMonth.IsEditable(new DateOnly(2026, month, 1), new DateOnly(2026, 11, 1)));

    [Fact]
    public void Of_ReturnsTheMonthOfADate() =>
        Assert.Equal(new DateOnly(2026, 11, 1), AllocationMonth.Of(new DateOnly(2026, 11, 30)));
}
