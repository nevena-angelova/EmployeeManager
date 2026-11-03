namespace EmployeeManager.Application.Allocations;

/// <summary>
/// A manager's year of hours: their direct reports' planned (allocated) and logged hours on the manager's projects,
/// month by month.
/// </summary>
/// <param name="Months">The team's totals for each month, January to December.</param>
/// <param name="Projects">The manager's projects, sorted by name.</param>
/// <param name="Members">The manager's direct reports, sorted by name.</param>
/// <param name="TotalPlanned">The year's planned hours on the manager's projects.</param>
/// <param name="TotalLogged">The year's logged hours on the manager's projects.</param>
public sealed record TeamStatisticsDto(
    int Year,
    IReadOnlyList<MonthStatisticsDto> Months,
    IReadOnlyList<ProjectStatisticsDto> Projects,
    IReadOnlyList<MemberStatisticsDto> Members,
    decimal TotalPlanned,
    decimal TotalLogged);

/// <param name="Month">The month, as <c>yyyy-MM</c>.</param>
/// <param name="CapacityHours">The hours each employee has available in the month.</param>
/// <param name="Planned">The direct reports' allocated hours on the manager's projects.</param>
/// <param name="Logged">The direct reports' logged hours on the manager's projects.</param>
/// <param name="OtherPlanned">The direct reports' allocated hours on other managers' projects.</param>
/// <param name="OtherLogged">The direct reports' logged hours on other managers' projects.</param>
public sealed record MonthStatisticsDto(
    string Month,
    decimal CapacityHours,
    decimal Planned,
    decimal Logged,
    decimal OtherPlanned,
    decimal OtherLogged);

/// <param name="Months">The direct reports' hours on the project, January to December.</param>
public sealed record ProjectStatisticsDto(
    Guid Id,
    string Name,
    IReadOnlyList<MonthHoursDto> Months,
    decimal TotalPlanned,
    decimal TotalLogged);

/// <param name="Months">The report's hours on the manager's projects, January to December.</param>
public sealed record MemberStatisticsDto(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string JobTitle,
    IReadOnlyList<MonthHoursDto> Months,
    decimal TotalPlanned,
    decimal TotalLogged);

public sealed record MonthHoursDto(string Month, decimal Planned, decimal Logged);
