using EmployeeManager.Application.Allocations;
using EmployeeManager.Application.Auth;
using EmployeeManager.Domain.Entities;
using EmployeeManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a sample org chart for local development, plus the roles, a sign-in account for every employee and a few
/// projects with this month's hours.
/// Each step is skipped if its data already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    /// <summary>The password of every seeded account. Development only.</summary>
    public const string DevelopmentPassword = "Passw0rd!";

    /// <summary>The seeded employee whose account is an Admin.</summary>
    public const string AdminEmail = "grace.turner@example.com";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<EmployeeManagerDbContext>();

        await SeedEmployeesAsync(db, cancellationToken);
        await SeedAdminEmployeeAsync(db, cancellationToken);
        await SeedRolesAsync(services.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
        await SeedUsersAsync(db, services.GetRequiredService<UserManager<ApplicationUser>>(), cancellationToken);
        await SeedProjectsAsync(db, cancellationToken);
        await SeedAllocationsAsync(db, services.GetRequiredService<TimeProvider>(), cancellationToken);
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

    /// <summary>Adds the admin under the CEO. A separate step so existing development databases get her too.</summary>
    private static async Task SeedAdminEmployeeAsync(EmployeeManagerDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Employees.AnyAsync(e => e.Email == AdminEmail, cancellationToken))
        {
            return;
        }

        var ceo = await db.Employees.FirstOrDefaultAsync(e => e.ManagerId == null, cancellationToken);
        var admin = Create("Grace", "Turner", "People Operations Administrator", new DateOnly(2019, 7, 1), ceo);

        db.Employees.Add(admin);
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

    /// <summary>Gives every employee without an account one: the admin as Admin, anyone else with direct reports as Manager.</summary>
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
            var role = employee.Email == AdminEmail ? Roles.Admin : Roles.ForEmployee(employee.HasDirectReports);
            ThrowIfFailed(await userManager.AddToRoleAsync(user, role));
        }
    }

    /// <summary>Gives a few managers a project staffed from their direct reports. Owners or members missing from the data are left out.</summary>
    private static async Task SeedProjectsAsync(EmployeeManagerDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Projects.AnyAsync(cancellationToken))
        {
            return;
        }

        var employeeIds = await db.Employees.ToDictionaryAsync(e => e.Email, e => e.Id, cancellationToken);

        (string Name, string Description, string Owner, string[] Members)[] projects =
        [
            ("Annual planning", "Set next year's goals and budget.", "margaret.hayes", ["daniel.okafor", "sofia.marin"]),
            ("Payments API", "Move card payments to the new provider.", "lucas.bennett", ["ethan.clarke", "hannah.lindqvist"]),
            ("Design system refresh", "Update the shared components and colours.", "priya.raman", ["chloe.dubois", "kenji.watanabe"]),
        ];

        foreach (var (name, description, owner, members) in projects)
        {
            if (!employeeIds.TryGetValue($"{owner}@example.com", out var ownerId))
            {
                continue;
            }

            db.Projects.Add(new Project
            {
                Name = name,
                Description = description,
                OwnerId = ownerId,
                Members = members
                    .Where(m => employeeIds.ContainsKey($"{m}@example.com"))
                    .Select(m => new ProjectMember { EmployeeId = employeeIds[$"{m}@example.com"] })
                    .ToList(),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Gives every seeded project member some hours on it this month.</summary>
    private static async Task SeedAllocationsAsync(
        EmployeeManagerDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (await db.HourAllocations.AnyAsync(cancellationToken))
        {
            return;
        }

        var month = AllocationMonth.Current(timeProvider);
        var members = await db.ProjectMembers
            .OrderBy(m => m.Project!.Name)
            .ThenBy(m => m.Employee!.FirstName)
            .Select(m => new { m.ProjectId, m.EmployeeId })
            .ToListAsync(cancellationToken);

        decimal[] hours = [80, 60, 120, 40.5m];
        db.HourAllocations.AddRange(members.Select((m, index) => new HourAllocation
        {
            ProjectId = m.ProjectId,
            EmployeeId = m.EmployeeId,
            Month = month,
            Hours = hours[index % hours.Length],
        }));

        await db.SaveChangesAsync(cancellationToken);
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
