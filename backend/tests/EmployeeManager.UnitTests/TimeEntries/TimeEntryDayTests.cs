using EmployeeManager.Application.TimeEntries;

namespace EmployeeManager.UnitTests.TimeEntries;

public class TimeEntryDayTests
{
    [Theory]
    [InlineData("2026-11-02", true)]
    [InlineData("2026-02-30", false)]
    [InlineData("2026-11", false)]
    [InlineData("02/11/2026", false)]
    [InlineData("", false)]
    public void TryParse_AcceptsOnlyIsoDates(string value, bool valid) =>
        Assert.Equal(valid, TimeEntryDay.TryParse(value, out _));

    [Fact]
    public void Format_WritesIsoDates() =>
        Assert.Equal("2026-11-02", TimeEntryDay.Format(new DateOnly(2026, 11, 2)));

    [Theory]
    [InlineData(2)] // Monday
    [InlineData(5)]
    [InlineData(8)] // Sunday
    public void WeekStart_IsTheMonday(int day) =>
        Assert.Equal(new DateOnly(2026, 11, 2), TimeEntryDay.WeekStart(new DateOnly(2026, 11, day)));

    [Fact]
    public void IsEditable_UpToToday()
    {
        var today = new DateOnly(2026, 11, 15);

        Assert.True(TimeEntryDay.IsEditable(today.AddDays(-30), today));
        Assert.True(TimeEntryDay.IsEditable(today, today));
        Assert.False(TimeEntryDay.IsEditable(today.AddDays(1), today));
    }
}
