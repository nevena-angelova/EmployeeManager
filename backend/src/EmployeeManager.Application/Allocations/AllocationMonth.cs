using System.Globalization;

namespace EmployeeManager.Application.Allocations;

/// <summary>Months that hours are allocated for, each represented by its first day.</summary>
public static class AllocationMonth
{
    public const decimal HoursPerWorkingDay = 8;

    /// <summary>Parses <c>yyyy-MM</c>, e.g. <c>2026-11</c>.</summary>
    public static bool TryParse(string? value, out DateOnly month) =>
        DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out month);

    /// <summary>Parses a four-digit year, e.g. <c>2026</c>. The last year <see cref="DateOnly"/> can hold is refused.</summary>
    public static bool TryParseYear(string? value, out int year) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out year)
        && value!.Length == 4
        && year is >= 1 and < 9999;

    public static string Format(DateOnly month) => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    /// <summary>The month <paramref name="date"/> falls in.</summary>
    public static DateOnly Of(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>The current month in UTC.</summary>
    public static DateOnly Current(TimeProvider timeProvider) =>
        Of(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

    /// <summary>The hours available in the month: a working day for every weekday. Public holidays aren't counted.</summary>
    public static decimal CapacityHours(DateOnly month)
    {
        var weekdays = Enumerable.Range(0, DateTime.DaysInMonth(month.Year, month.Month))
            .Select(month.AddDays)
            .Count(day => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));
        return weekdays * HoursPerWorkingDay;
    }

    /// <summary>The current and later months can be changed; past months are kept as they were.</summary>
    public static bool IsEditable(DateOnly month, DateOnly currentMonth) => month >= currentMonth;

    /// <summary>E.g. "November 2026", for messages.</summary>
    public static string Describe(DateOnly month) => month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}
