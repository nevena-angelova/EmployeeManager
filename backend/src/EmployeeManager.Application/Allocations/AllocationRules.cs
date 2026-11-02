namespace EmployeeManager.Application.Allocations;

public static class AllocationRules
{
    /// <summary>Hours are allocated in steps of half an hour.</summary>
    public const decimal Step = 0.5m;

    /// <summary>
    /// Returns why a month's allocations can't be saved as a whole, or null if they can: each employee must be one
    /// of the manager's <paramref name="directReports"/> and appear at most once per project.
    /// </summary>
    public static string? ValidateRequest(IEnumerable<SaveAllocationItem> allocations, IReadOnlySet<Guid> directReports)
    {
        var items = allocations.ToList();
        if (items.Any(a => !directReports.Contains(a.EmployeeId)))
        {
            return "Hours can only be allocated to your direct reports.";
        }

        return items.DistinctBy(a => (a.EmployeeId, a.ProjectId)).Count() == items.Count
            ? null
            : "Each person can only have one allocation per project.";
    }

    /// <summary>
    /// Returns why one employee's <paramref name="hours"/> for a month can't be saved, or null if they can. Each
    /// project must be one of the manager's that the employee is on (<paramref name="allowedProjectIds"/>), and
    /// together with their <paramref name="otherHours"/> on other managers' projects the hours must fit the month.
    /// </summary>
    public static string? ValidateEmployee(
        string employeeName,
        IReadOnlyCollection<ProjectHoursDto> hours,
        IReadOnlySet<Guid> allowedProjectIds,
        decimal otherHours,
        DateOnly month)
    {
        if (hours.Any(h => h.Hours < 0))
        {
            return "Hours can't be negative.";
        }

        if (hours.Any(h => h.Hours % Step != 0))
        {
            return "Hours must be in steps of half an hour.";
        }

        if (hours.Any(h => h.Hours > 0 && !allowedProjectIds.Contains(h.ProjectId)))
        {
            return $"{employeeName} isn't on one of the projects.";
        }

        var total = hours.Sum(h => h.Hours) + otherHours;
        var capacity = AllocationMonth.CapacityHours(month);
        return total <= capacity
            ? null
            : $"{employeeName} would have {total:0.#}h; {AllocationMonth.Describe(month)} has {capacity:0.#}h available.";
    }
}
