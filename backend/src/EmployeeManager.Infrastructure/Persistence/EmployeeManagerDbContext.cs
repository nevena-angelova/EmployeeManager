using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Domain.Common;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure.Persistence;

public class EmployeeManagerDbContext(DbContextOptions<EmployeeManagerDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Employee> Employees => Set<Employee>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
