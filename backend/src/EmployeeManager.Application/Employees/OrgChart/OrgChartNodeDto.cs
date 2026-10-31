namespace EmployeeManager.Application.Employees.OrgChart;

public sealed record OrgChartNodeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string JobTitle,
    string Email,
    IReadOnlyList<OrgChartNodeDto> DirectReports);
