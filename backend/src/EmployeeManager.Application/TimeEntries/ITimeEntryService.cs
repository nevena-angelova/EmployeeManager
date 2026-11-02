using EmployeeManager.Application.Employees.Management;

namespace EmployeeManager.Application.TimeEntries;

/// <summary>The hours employees log each day on the projects they're on.</summary>
public interface ITimeEntryService
{
    /// <summary>The employee's hours on the day on every project they're on or have hours on, and the week's totals.</summary>
    Task<DayTimeEntriesDto> GetDayAsync(DateOnly date, Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the employee's hours on the day. Days after today, and losing a race with another save, give a
    /// conflict; rule violations are keyed <c>entries</c>.
    /// </summary>
    Task<EmployeeResult<DayTimeEntriesDto>> SaveDayAsync(
        DateOnly date,
        SaveDayTimeEntriesRequest request,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
