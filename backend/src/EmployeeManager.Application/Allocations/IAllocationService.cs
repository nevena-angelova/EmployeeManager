using EmployeeManager.Application.Employees.Management;

namespace EmployeeManager.Application.Allocations;

/// <summary>
/// Monthly hours that managers allocate to their direct reports on the manager's projects. <c>month</c> is always
/// the month's first day.
/// </summary>
public interface IAllocationService
{
    /// <summary>The manager's direct reports and their hours on the manager's projects in the month.</summary>
    Task<TeamAllocationsDto> GetTeamAsync(DateOnly month, Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the month's hours for the manager's direct reports on the manager's projects. Past months give a
    /// conflict; rule violations are keyed <c>allocations</c>, or <c>allocations.{employeeId}</c> for one employee.
    /// </summary>
    Task<EmployeeResult<TeamAllocationsDto>> SaveTeamAsync(
        DateOnly month,
        SaveTeamAllocationsRequest request,
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The year's planned and logged hours of the manager's direct reports on the manager's projects, by month,
    /// project and report.
    /// </summary>
    Task<TeamStatisticsDto> GetTeamStatisticsAsync(int year, Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>The employee's hours in the month on every project they're on or have hours on.</summary>
    Task<MyAllocationsDto> GetMineAsync(DateOnly month, Guid employeeId, CancellationToken cancellationToken = default);
}
