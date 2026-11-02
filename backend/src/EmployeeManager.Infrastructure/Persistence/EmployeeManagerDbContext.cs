using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Domain.Common;
using EmployeeManager.Domain.Entities;
using EmployeeManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure.Persistence;

public class EmployeeManagerDbContext(DbContextOptions<EmployeeManagerDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public const string CaseInsensitiveCollation = "case_insensitive";

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Members are rows of their own, so adding or removing one leaves the project unmodified. Count it as a change
        // to the project, so its UpdatedAt is stamped below.
        var changedProjectIds = ChangeTracker.Entries<ProjectMember>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted)
            .Select(e => e.Entity.ProjectId)
            .ToHashSet();
        foreach (var project in ChangeTracker.Entries<Project>())
        {
            if (project.State == EntityState.Unchanged && changedProjectIds.Contains(project.Entity.Id))
            {
                project.Property(p => p.UpdatedAt).IsModified = true;
            }
        }

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
        // Identity's mapping goes first so our configurations can extend it (e.g. ApplicationUser's employee link).
        base.OnModelCreating(modelBuilder);

        // Compares text ignoring case (and nothing else), e.g. so unique emails are unique regardless of case.
        modelBuilder.HasCollation(
            CaseInsensitiveCollation,
            locale: "und-u-ks-level2",
            provider: "icu",
            deterministic: false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeManagerDbContext).Assembly);
    }
}
