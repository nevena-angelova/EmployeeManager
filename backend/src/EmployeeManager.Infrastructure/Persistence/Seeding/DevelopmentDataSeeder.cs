using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a sample org chart for local development. Does nothing if any employees already exist.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(EmployeeManagerDbContext db, CancellationToken cancellationToken = default)
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
