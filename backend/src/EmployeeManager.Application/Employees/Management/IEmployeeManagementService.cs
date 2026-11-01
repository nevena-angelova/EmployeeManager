namespace EmployeeManager.Application.Employees.Management;

/// <summary>Admin use cases for employees and their sign-in accounts.</summary>
public interface IEmployeeManagementService
{
    /// <summary>Returns every employee, sorted by first name, then last name.</summary>
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates the employee and an account with <see cref="CreateEmployeeRequest.Password"/> and the requested role.</summary>
    Task<EmployeeResult<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates the employee and keeps their account's email and role in step. <paramref name="currentEmployeeId"/> is the signed-in admin.</summary>
    Task<EmployeeResult<EmployeeDto>> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the employee and their account. Fails if they still have direct reports or are the signed-in admin.</summary>
    Task<EmployeeResult> DeleteAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default);
}
