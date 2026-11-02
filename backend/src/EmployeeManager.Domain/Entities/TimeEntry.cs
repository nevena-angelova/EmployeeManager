using EmployeeManager.Domain.Common;

namespace EmployeeManager.Domain.Entities;

/// <summary>
/// The hours an employee spent on a project on one day, logged by the employee. Like <see cref="HourAllocation"/>, it
/// refers to the project and employee rather than the membership, so it's kept when the employee leaves the project.
/// </summary>
public class TimeEntry : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Always positive: a day with zero hours on a project is not stored.</summary>
    public decimal Hours { get; set; }
}
