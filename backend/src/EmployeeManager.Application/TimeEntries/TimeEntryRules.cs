namespace EmployeeManager.Application.TimeEntries;

public static class TimeEntryRules
{
    /// <summary>Hours are logged in steps of half an hour.</summary>
    public const decimal Step = 0.5m;

    /// <summary>No one can log more hours than a day has.</summary>
    public const decimal MaxHoursPerDay = 24;

    /// <summary>
    /// Returns why a day's <paramref name="entries"/> can't be saved, or null if they can. Each project must appear at
    /// most once and be one the employee can log hours on (<paramref name="allowedProjectIds"/>), and the total must
    /// fit in a day.
    /// </summary>
    public static string? Validate(IReadOnlyCollection<SaveTimeEntryItem> entries, IReadOnlySet<Guid> allowedProjectIds)
    {
        if (entries.DistinctBy(e => e.ProjectId).Count() != entries.Count)
        {
            return "Each project can only appear once.";
        }

        if (entries.Any(e => e.Hours < 0))
        {
            return "Hours can't be negative.";
        }

        if (entries.Any(e => e.Hours % Step != 0))
        {
            return "Hours must be in steps of half an hour.";
        }

        if (entries.Any(e => e.Hours > 0 && !allowedProjectIds.Contains(e.ProjectId)))
        {
            return "You can only log hours on projects you're on.";
        }

        var total = entries.Sum(e => e.Hours);
        return total <= MaxHoursPerDay
            ? null
            : $"That's {total:0.#}h; a day has at most {MaxHoursPerDay:0}h.";
    }
}
