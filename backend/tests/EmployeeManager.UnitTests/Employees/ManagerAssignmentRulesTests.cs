using EmployeeManager.Application.Employees.Management;

namespace EmployeeManager.UnitTests.Employees;

public class ManagerAssignmentRulesTests
{
    private static readonly Guid Ceo = Guid.NewGuid();
    private static readonly Guid Director = Guid.NewGuid();
    private static readonly Guid Lead = Guid.NewGuid();
    private static readonly Guid Developer = Guid.NewGuid();
    private static readonly Guid OtherDirector = Guid.NewGuid();

    // Ceo → Director → Lead → Developer, and Ceo → OtherDirector.
    private static readonly Dictionary<Guid, Guid?> Hierarchy = new()
    {
        [Ceo] = null,
        [Director] = Ceo,
        [Lead] = Director,
        [Developer] = Lead,
        [OtherDirector] = Ceo,
    };

    [Fact]
    public void Validate_WithNoManager_IsValid() =>
        Assert.Null(ManagerAssignmentRules.Validate(Director, null, Hierarchy));

    [Fact]
    public void Validate_ForNewEmployeeWithExistingManager_IsValid() =>
        Assert.Null(ManagerAssignmentRules.Validate(null, Lead, Hierarchy));

    [Fact]
    public void Validate_MovingToAnotherBranch_IsValid() =>
        Assert.Null(ManagerAssignmentRules.Validate(Lead, OtherDirector, Hierarchy));

    [Fact]
    public void Validate_WithUnknownManager_ReturnsError() =>
        Assert.Contains("doesn't exist", ManagerAssignmentRules.Validate(Lead, Guid.NewGuid(), Hierarchy));

    [Fact]
    public void Validate_WithSelfAsManager_ReturnsError() =>
        Assert.Contains("own manager", ManagerAssignmentRules.Validate(Lead, Lead, Hierarchy));

    [Fact]
    public void Validate_WithDirectReportAsManager_ReturnsError() =>
        Assert.Contains("loop", ManagerAssignmentRules.Validate(Director, Lead, Hierarchy));

    [Fact]
    public void Validate_WithIndirectReportAsManager_ReturnsError() =>
        Assert.Contains("loop", ManagerAssignmentRules.Validate(Ceo, Developer, Hierarchy));

    [Fact]
    public void Validate_WithExistingCycleAboveTheManager_Terminates()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var employee = Guid.NewGuid();
        var managerOf = new Dictionary<Guid, Guid?> { [a] = b, [b] = a, [employee] = null };

        Assert.Null(ManagerAssignmentRules.Validate(employee, a, managerOf));
    }
}
