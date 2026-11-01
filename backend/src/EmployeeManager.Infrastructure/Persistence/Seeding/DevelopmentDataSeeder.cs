using EmployeeManager.Application.Auth;
using EmployeeManager.Domain.Entities;
using EmployeeManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a sample org chart for local development, plus the roles and a sign-in account for every employee.
/// Each step is skipped if its data already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    /// <summary>The password of every seeded account. Development only.</summary>
    public const string DevelopmentPassword = "Passw0rd!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<EmployeeManagerDbContext>();

        await SeedEmployeesAsync(db, cancellationToken);
        await SeedRolesAsync(services.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
        await SeedUsersAsync(db, services.GetRequiredService<UserManager<ApplicationUser>>(), cancellationToken);
    }

    private static async Task SeedEmployeesAsync(EmployeeManagerDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Employees.AnyAsync(cancellationToken))
        {
            return;
        }

        // Top level: CEO, with two directors who manage team leads.
        var ceo = Create("Margaret", "Hayes", "Chief Executive Officer", new DateOnly(2015, 3, 2), manager: null);

        var engineeringDirector = Create("Daniel", "Okafor", "Director of Engineering", new DateOnly(2017, 6, 12), ceo);
        var operationsDirector = Create("Sofia", "Marin", "Director of Operations", new DateOnly(2018, 1, 8), ceo);

        var backendLead = Create("Lucas", "Bennett", "Backend Team Lead", new DateOnly(2019, 4, 15), engineeringDirector);
        var frontendLead = Create("Priya", "Raman", "Frontend Team Lead", new DateOnly(2019, 9, 23), engineeringDirector);
        var supportLead = Create("Tomas", "Novak", "Customer Support Lead", new DateOnly(2020, 2, 3), operationsDirector);

        Employee[] employees =
        [
            ceo,
            engineeringDirector,
            operationsDirector,
            backendLead,
            frontendLead,
            supportLead,
            Create("Ethan", "Clarke", "Backend Developer", new DateOnly(2021, 5, 10), backendLead),
            Create("Hannah", "Lindqvist", "Backend Developer", new DateOnly(2022, 8, 1), backendLead),
            Create("Omar", "Haddad", "QA Engineer", new DateOnly(2023, 3, 20), backendLead),
            Create("Chloe", "Dubois", "Frontend Developer", new DateOnly(2021, 11, 15), frontendLead),
            Create("Kenji", "Watanabe", "UI/UX Designer", new DateOnly(2022, 2, 7), frontendLead),
            Create("Isabel", "Costa", "Support Specialist", new DateOnly(2022, 6, 13), supportLead),
            Create("Liam", "O'Connor", "Support Specialist", new DateOnly(2023, 9, 4), supportLead),
            Create("Amara", "Nwosu", "Office Administrator", new DateOnly(2021, 1, 18), operationsDirector),
            Create("Felix", "Weber", "Executive Assistant", new DateOnly(2020, 10, 5), ceo),
        ];

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                ThrowIfFailed(await roleManager.CreateAsync(new IdentityRole<Guid>(role)));
            }
        }
    }

    /// <summary>Gives every employee without an account one, as a Manager if they have direct reports.</summary>
    private static async Task SeedUsersAsync(
        EmployeeManagerDbContext db,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var employees = await db.Employees
            .Where(e => !db.Users.Any(u => u.EmployeeId == e.Id))
            .Select(e => new { e.Id, e.Email, HasDirectReports = e.DirectReports.Any() })
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            var user = new ApplicationUser
            {
                UserName = employee.Email,
                Email = employee.Email,
                EmailConfirmed = true,
                EmployeeId = employee.Id,
            };

            ThrowIfFailed(await userManager.CreateAsync(user, DevelopmentPassword));
            ThrowIfFailed(await userManager.AddToRoleAsync(user, Roles.ForEmployee(employee.HasDirectReports)));
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Seeding failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static Employee Create(string firstName, string lastName, string jobTitle, DateOnly hireDate, Employee? manager) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName}.{lastName}".Replace("'", "").ToLowerInvariant() + "@example.com",
            JobTitle = jobTitle,
            HireDate = hireDate,
            Manager = manager,
        };
}
