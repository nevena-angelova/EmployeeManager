using EmployeeManager.Domain.Common;

namespace EmployeeManager.Domain.Entities;

public class Employee : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string JobTitle { get; set; }
    public DateOnly HireDate { get; set; }

    /// <summary>Null for top-level managers who report to no one.</summary>
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = [];
}
