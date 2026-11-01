using EmployeeManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EmployeeManager.Infrastructure.Identity;

/// <summary>A sign-in account. Each one belongs to exactly one employee, and its user name is the employee's email.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
}
