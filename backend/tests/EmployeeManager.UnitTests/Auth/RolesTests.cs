using EmployeeManager.Application.Auth;

namespace EmployeeManager.UnitTests.Auth;

public class RolesTests
{
    [Theory]
    [InlineData(true, Roles.Manager)]
    [InlineData(false, Roles.Employee)]
    public void ForEmployee_IsManagerOnlyWithDirectReports(bool hasDirectReports, string expected) =>
        Assert.Equal(expected, Roles.ForEmployee(hasDirectReports));

    [Fact]
    public void All_ContainsEmployeeAndManager() =>
        Assert.Equal([Roles.Employee, Roles.Manager], Roles.All);
}
