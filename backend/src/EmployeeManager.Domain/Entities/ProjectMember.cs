using EmployeeManager.Domain.Common;

namespace EmployeeManager.Domain.Entities;

/// <summary>An employee assigned to a project. <see cref="BaseEntity.CreatedAt"/> is when they were assigned.</summary>
public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
