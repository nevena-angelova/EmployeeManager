using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        // Case-insensitive, so one manager can't have both "Billing" and "billing".
        builder.Property(p => p.Name)
            .HasMaxLength(150)
            .UseCollation(EmployeeManagerDbContext.CaseInsensitiveCollation);
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasIndex(p => new { p.OwnerId, p.Name }).IsUnique();

        // A deleted manager's projects go with them.
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
