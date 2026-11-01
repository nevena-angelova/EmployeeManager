using EmployeeManager.Application.Auth;
using EmployeeManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    EmployeeManagerDbContext db,
    JwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<LoginResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Counts towards Identity's lockout (5 failures locks the account for 5 minutes by default).
            await userManager.AccessFailedAsync(user);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var currentUser = await GetCurrentUserAsync(user.Id, cancellationToken);
        if (currentUser is null)
        {
            return null;
        }

        var (token, expiresAt) = tokenGenerator.Generate(user, currentUser.Role);
        return new LoginResultDto(token, expiresAt, currentUser);
    }

    public Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new CurrentUserDto(
                u.EmployeeId,
                u.Employee.Email,
                u.Employee.FirstName,
                u.Employee.LastName,
                u.Employee.JobTitle,
                // A user without a role gets the least privileged one.
                db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                    .FirstOrDefault() ?? Roles.Employee))
            .FirstOrDefaultAsync(cancellationToken);
}
