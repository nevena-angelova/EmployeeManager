namespace EmployeeManager.Api;

/// <summary>Named authorization policies. Every endpoint already requires a signed-in user unless it opts out with <c>[AllowAnonymous]</c>.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Only users in the Manager role. Use as <c>[Authorize(Policy = AuthorizationPolicies.ManagerOnly)]</c>.</summary>
    public const string ManagerOnly = nameof(ManagerOnly);

    /// <summary>Only users in the Admin role. Use as <c>[Authorize(Policy = AuthorizationPolicies.AdminOnly)]</c>.</summary>
    public const string AdminOnly = nameof(AdminOnly);
}
