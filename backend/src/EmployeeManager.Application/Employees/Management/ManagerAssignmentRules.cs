namespace EmployeeManager.Application.Employees.Management;

public static class ManagerAssignmentRules
{
    /// <summary>
    /// Returns why <paramref name="managerId"/> can't manage <paramref name="employeeId"/> (null for a new employee),
    /// or null if the assignment is fine. <paramref name="managerOf"/> maps every existing employee to their manager.
    /// </summary>
    public static string? Validate(Guid? employeeId, Guid? managerId, IReadOnlyDictionary<Guid, Guid?> managerOf)
    {
        if (managerId is not { } candidate)
        {
            return null;
        }

        if (!managerOf.ContainsKey(candidate))
        {
            return "The selected manager doesn't exist.";
        }

        if (candidate == employeeId)
        {
            return "An employee can't be their own manager.";
        }

        // Walk up from the proposed manager. Meeting the employee means the manager reports to them, directly or
        // not, which would create a loop. The visited set stops at loops already in the data.
        var visited = new HashSet<Guid>();
        for (Guid? current = candidate; current is { } id && visited.Add(id); current = managerOf.GetValueOrDefault(id))
        {
            if (id == employeeId)
            {
                return "The selected manager reports to this employee, which would create a reporting loop.";
            }
        }

        return null;
    }
}
