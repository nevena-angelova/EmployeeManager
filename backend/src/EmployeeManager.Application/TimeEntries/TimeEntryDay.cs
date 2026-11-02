using System.Globalization;

namespace EmployeeManager.Application.TimeEntries;

/// <summary>Days that employees log hours for.</summary>
public static class TimeEntryDay
{
    /// <summary>Parses <c>yyyy-MM-dd</c>, e.g. <c>2026-11-02</c>.</summary>
    public static bool TryParse(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Today in UTC.</summary>
    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    /// <summary>The Monday of the week <paramref name="date"/> falls in.</summary>
    public static DateOnly WeekStart(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>Hours can be logged for today and earlier days, but not for days that haven't happened yet.</summary>
    public static bool IsEditable(DateOnly date, DateOnly today) => date <= today;
}
