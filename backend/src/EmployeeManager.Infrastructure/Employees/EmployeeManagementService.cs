using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Domain.Entities;
using EmployeeManager.Infrastructure.Identity;
using EmployeeManager.Infrastructure.Persistence;
using EmployeeManager.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EmployeeManager.Infrastructure.Employees;

/// <summary>
/// Lives in Infrastructure, like <see cref="AuthService"/>, because every write also changes the employee's
/// Identity account. Each write runs in one transaction so an employee never exists without their account.
/// </summary>
public class EmployeeManagementService(
    EmployeeManagerDbContext db,
    UserManager<ApplicationUser> userManager) : IEmployeeManagementService
{
    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await Project(db.Employees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName))
            .ToListAsync(cancellationToken);

    public Task<EmployeeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Project(db.Employees.Where(e => e.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<EmployeeResult<EmployeeDto>> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var invalid = await ValidateAsync(null, email, request.ManagerId, request.Role, cancellationToken);
        if (invalid is not null)
        {
            return EmployeeResult<EmployeeDto>.From(invalid);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            JobTitle = request.JobTitle.Trim(),
            HireDate = request.HireDate,
            ManagerId = request.ManagerId,
        };
        db.Employees.Add(employee);
        if (await SaveEmployeeAsync(cancellationToken) is { } violation)
        {
            return EmployeeResult<EmployeeDto>.From(violation);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            EmployeeId = employee.Id,
        };

        var identityResult = await userManager.CreateAsync(user, request.Password);
        if (identityResult.Succeeded)
        {
            identityResult = await userManager.AddToRoleAsync(user, request.Role);
        }

        if (!identityResult.Succeeded)
        {
            // Nothing is committed; forget the tracked employee so the context stays usable.
            db.ChangeTracker.Clear();
            return EmployeeResult<EmployeeDto>.From(EmployeeResult.Invalid(ToFieldErrors(identityResult)));
        }

        await transaction.CommitAsync(cancellationToken);
        return EmployeeResult<EmployeeDto>.Success((await GetAsync(employee.Id, cancellationToken))!);
    }

    public async Task<EmployeeResult<EmployeeDto>> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        Guid currentEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return EmployeeResult<EmployeeDto>.From(EmployeeResult.NotFound());
        }

        var email = request.Email.Trim();
        var invalid = await ValidateAsync(id, email, request.ManagerId, request.Role, cancellationToken);
        if (invalid is not null)
        {
            return EmployeeResult<EmployeeDto>.From(invalid);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == id, cancellationToken);
        var currentRoles = user is null ? [] : await userManager.GetRolesAsync(user);

        // An admin demoting themselves could leave nobody able to manage employees.
        if (id == currentEmployeeId && currentRoles.Contains(Roles.Admin) && request.Role != Roles.Admin)
        {
            return EmployeeResult<EmployeeDto>.From(
                EmployeeResult.Invalid("role", "You can't remove the Admin role from your own account."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.JobTitle = request.JobTitle.Trim();
        employee.HireDate = request.HireDate;
        employee.ManagerId = request.ManagerId;
        if (await SaveEmployeeAsync(cancellationToken) is { } violation)
        {
            return EmployeeResult<EmployeeDto>.From(violation);
        }

        if (user is not null)
        {
            var identityResult = IdentityResult.Success;
            if (user.Email != email)
            {
                user.Email = email;
                user.UserName = email;
                identityResult = await userManager.UpdateAsync(user);
            }

            if (identityResult.Succeeded && !currentRoles.SequenceEqual([request.Role]))
            {
                identityResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (identityResult.Succeeded)
                {
                    identityResult = await userManager.AddToRoleAsync(user, request.Role);
                }
            }

            if (!identityResult.Succeeded)
            {
                db.ChangeTracker.Clear();
                return EmployeeResult<EmployeeDto>.From(EmployeeResult.Invalid(ToFieldErrors(identityResult)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return EmployeeResult<EmployeeDto>.Success((await GetAsync(id, cancellationToken))!);
    }

    public async Task<EmployeeResult> DeleteAsync(Guid id, Guid currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return EmployeeResult.NotFound();
        }

        if (id == currentEmployeeId)
        {
            return EmployeeResult.Conflict("You can't delete your own account.");
        }

        var directReports = await db.Employees.CountAsync(e => e.ManagerId == id, cancellationToken);
        if (directReports > 0)
        {
            return EmployeeResult.Conflict(
                $"{employee.FirstName} {employee.LastName} still has {directReports} direct " +
                $"{(directReports == 1 ? "report" : "reports")}. Assign them to another manager first.");
        }

        // The account is removed with the employee by the cascade on users.employee_id.
        db.Employees.Remove(employee);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsViolation(ex, PostgresErrorCodes.RestrictViolation, EmployeeConfiguration.ManagerForeignKeyName))
        {
            // A report was assigned to them after the check above.
            db.ChangeTracker.Clear();
            return EmployeeResult.Conflict(
                $"{employee.FirstName} {employee.LastName} has direct reports. Assign them to another manager first.");
        }

        return EmployeeResult.Success();
    }

    /// <summary>Checks the rules data annotations can't: a known role, a free email and a valid manager.</summary>
    private async Task<EmployeeResult?> ValidateAsync(
        Guid? id,
        string email,
        Guid? managerId,
        string role,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Roles.All.Contains(role))
        {
            errors["role"] = [$"Role must be one of: {string.Join(", ", Roles.All)}."];
        }

        // The email column's collation makes this comparison case-insensitive, and it can use the unique index.
        if (await db.Employees.AnyAsync(e => e.Id != id && e.Email == email, cancellationToken))
        {
            errors["email"] = ["Another employee already uses this email."];
        }

        if (managerId is not null)
        {
            var managerOf = await db.Employees
                .AsNoTracking()
                .ToDictionaryAsync(e => e.Id, e => e.ManagerId, cancellationToken);

            if (ManagerAssignmentRules.Validate(id, managerId, managerOf) is { } managerError)
            {
                errors["managerId"] = [managerError];
            }
        }

        return errors.Count == 0 ? null : EmployeeResult.Invalid(errors);
    }

    private IQueryable<EmployeeDto> Project(IQueryable<Employee> employees) =>
        employees
            .AsNoTracking()
            .Select(e => new EmployeeDto(
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.JobTitle,
                e.HireDate,
                e.ManagerId,
                e.Manager == null ? null : e.Manager.FirstName + " " + e.Manager.LastName,
                // An employee without an account or role counts as the least privileged role, as at sign-in.
                (from u in db.Users
                    where u.EmployeeId == e.Id
                    join ur in db.UserRoles on u.Id equals ur.UserId
                    join r in db.Roles on ur.RoleId equals r.Id
                    select r.Name).FirstOrDefault() ?? Roles.Employee));

    /// <summary>
    /// Saves the employee change, or returns the validation error for a constraint it broke (see
    /// <see cref="ToValidationError"/>) after forgetting the tracked change so the context stays usable.
    /// </summary>
    private async Task<EmployeeResult?> SaveEmployeeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException ex) when (ToValidationError(ex) is { } violation)
        {
            db.ChangeTracker.Clear();
            return violation;
        }
    }

    /// <summary>
    /// Maps a constraint violation from a concurrent change that got past <see cref="ValidateAsync"/> (the same email
    /// saved, or the manager deleted, in between) to the error the check would have given. Null for anything else.
    /// </summary>
    private static EmployeeResult? ToValidationError(DbUpdateException exception) =>
        IsViolation(exception, PostgresErrorCodes.UniqueViolation, EmployeeConfiguration.EmailIndexName)
            ? EmployeeResult.Invalid("email", "Another employee already uses this email.")
            : IsViolation(exception, PostgresErrorCodes.ForeignKeyViolation, EmployeeConfiguration.ManagerForeignKeyName)
                ? EmployeeResult.Invalid("managerId", "The selected manager doesn't exist.")
                : null;

    private static bool IsViolation(DbUpdateException exception, string sqlState, string constraintName) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == sqlState
        && postgres.ConstraintName == constraintName;

    /// <summary>Files Identity's errors under the form field they're about.</summary>
    private static Dictionary<string, string[]> ToFieldErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password"
                : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "email"
                : "role")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
