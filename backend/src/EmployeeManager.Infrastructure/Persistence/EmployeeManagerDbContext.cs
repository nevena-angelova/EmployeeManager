using EmployeeManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure.Persistence;

public class EmployeeManagerDbContext(DbContextOptions<EmployeeManagerDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
