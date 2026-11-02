using EmployeeManager.Domain.Common;

namespace EmployeeManager.Domain.Entities;

/// <summary>
/// The hours an employee is to spend on a project in one month, set by the project's owner. It refers to the project
/// and employee rather than the membership, so past months are kept when the employee leaves the project.
/// </summary>
public class HourAllocation : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>The first day of the month.</summary>
    public DateOnly Month { get; set; }

    /// <summary>Always positive: an allocation of zero hours is not stored.</summary>
    public decimal Hours { get; set; }
}
