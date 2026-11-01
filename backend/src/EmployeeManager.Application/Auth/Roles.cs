namespace EmployeeManager.Application.Auth;

/// <summary>The authorization roles a signed-in user can hold. Each user has exactly one.</summary>
public static class Roles
{
    public const string Employee = "Employee";
    public const string Manager = "Manager";

    public static IReadOnlyList<string> All { get; } = [Employee, Manager];

    /// <summary>Anyone with direct reports is a manager; everyone else is an employee.</summary>
    public static string ForEmployee(bool hasDirectReports) => hasDirectReports ? Manager : Employee;
}
