using EmployeeManager.Domain.Common;

namespace EmployeeManager.Domain.Entities;

/// <summary>A piece of work a manager staffs from their direct reports.</summary>
public class Project : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>The manager who created the project and can change it.</summary>
    public Guid OwnerId { get; set; }
    public Employee? Owner { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = [];
}
