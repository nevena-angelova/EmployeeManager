namespace EmployeeManager.Application.TimeEntries;

/// <summary>The signed-in employee's hours on one day.</summary>
/// <param name="Date">The day, as <c>yyyy-MM-dd</c>.</param>
/// <param name="IsEditable">False for days after today, which can't have hours yet.</param>
/// <param name="Projects">
/// The projects they can log hours on that day, sorted by name: the ones they're on, plus any they already have hours
/// on that day.
/// </param>
/// <param name="Week">The total for each day of the week (Monday to Sunday) the day falls in.</param>
public sealed record DayTimeEntriesDto(
    string Date,
    bool IsEditable,
    decimal MaxHours,
    IReadOnlyList<DayProjectHoursDto> Projects,
    decimal TotalHours,
    IReadOnlyList<DayTotalDto> Week);

public sealed record DayProjectHoursDto(Guid ProjectId, string ProjectName, string OwnerName, decimal Hours);

/// <param name="Date">The day, as <c>yyyy-MM-dd</c>.</param>
public sealed record DayTotalDto(string Date, decimal Hours);

/// <summary>Replaces the employee's hours on one day. Projects left out, or given zero hours, are removed.</summary>
public sealed record SaveDayTimeEntriesRequest(IReadOnlyList<SaveTimeEntryItem>? Entries);

public sealed record SaveTimeEntryItem(Guid ProjectId, decimal Hours);
