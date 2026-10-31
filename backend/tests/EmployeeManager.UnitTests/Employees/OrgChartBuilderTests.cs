using EmployeeManager.Application.Employees.OrgChart;

namespace EmployeeManager.UnitTests.Employees;

public class OrgChartBuilderTests
{
    [Fact]
    public void Build_WithNoEmployees_ReturnsEmptyList()
    {
        var result = OrgChartBuilder.Build([]);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Build_NestsReportsUnderTheirManagers()
    {
        var ceo = Employee("Margaret", "Hayes");
        var director = Employee("Daniel", "Okafor", ceo);
        var developer = Employee("Ethan", "Clarke", director);
        var otherRoot = Employee("Zoe", "Adams");

        var result = OrgChartBuilder.Build([developer, director, ceo, otherRoot])!;

        Assert.Equal([ceo.Id, otherRoot.Id], result.Select(n => n.Id));
        var directorNode = Assert.Single(result[0].DirectReports);
        Assert.Equal(director.Id, directorNode.Id);
        var developerNode = Assert.Single(directorNode.DirectReports);
        Assert.Equal(developer.Id, developerNode.Id);
        Assert.Empty(developerNode.DirectReports);
        Assert.Empty(result[1].DirectReports);
    }

    [Fact]
    public void Build_OrdersEachLevelByFirstNameThenLastName()
    {
        var ceo = Employee("Margaret", "Hayes");
        var sofia = Employee("Sofia", "Marin", ceo);
        var annaZ = Employee("Anna", "Zeller", ceo);
        var annaB = Employee("Anna", "Brown", ceo);
        var rootB = Employee("Bob", "Young");

        var result = OrgChartBuilder.Build([sofia, annaZ, ceo, annaB, rootB])!;

        Assert.Equal(["Bob", "Margaret"], result.Select(n => n.FirstName));
        Assert.Equal([annaB.Id, annaZ.Id, sofia.Id], result[1].DirectReports.Select(n => n.Id));
    }

    [Fact]
    public void Build_WithRootId_ReturnsOnlyThatSubtree()
    {
        var ceo = Employee("Margaret", "Hayes");
        var engineering = Employee("Daniel", "Okafor", ceo);
        var operations = Employee("Sofia", "Marin", ceo);
        var developer = Employee("Ethan", "Clarke", engineering);

        var result = OrgChartBuilder.Build([ceo, engineering, operations, developer], engineering.Id)!;

        var root = Assert.Single(result);
        Assert.Equal(engineering.Id, root.Id);
        Assert.Equal(developer.Id, Assert.Single(root.DirectReports).Id);
    }

    [Fact]
    public void Build_WithUnknownRootId_ReturnsNull()
    {
        var result = OrgChartBuilder.Build([Employee("Margaret", "Hayes")], Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Build_WithCycleNotReachableFromARoot_OmitsIt()
    {
        var ceo = Employee("Margaret", "Hayes");
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var a = Employee("Alice", "Cycle", managerId: bId, id: aId);
        var b = Employee("Bob", "Cycle", managerId: aId, id: bId);

        var result = OrgChartBuilder.Build([ceo, a, b])!;

        var root = Assert.Single(result);
        Assert.Equal(ceo.Id, root.Id);
        Assert.Empty(root.DirectReports);
    }

    [Fact]
    public void Build_WithRootIdInsideCycle_CutsTheCycle()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var a = Employee("Alice", "Cycle", managerId: bId, id: aId);
        var b = Employee("Bob", "Cycle", managerId: aId, id: bId);

        var result = OrgChartBuilder.Build([a, b], aId)!;

        var root = Assert.Single(result);
        Assert.Equal(aId, root.Id);
        var child = Assert.Single(root.DirectReports);
        Assert.Equal(bId, child.Id);
        Assert.Empty(child.DirectReports);
    }

    private static OrgChartEmployee Employee(string firstName, string lastName, OrgChartEmployee? manager = null) =>
        Employee(firstName, lastName, manager?.Id);

    private static OrgChartEmployee Employee(string firstName, string lastName, Guid? managerId, Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            managerId,
            firstName,
            lastName,
            "Job Title",
            $"{firstName}.{lastName}@example.com".ToLowerInvariant());
}
