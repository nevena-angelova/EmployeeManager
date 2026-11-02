using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("time_entries", table =>
        {
            table.HasCheckConstraint("ck_time_entries_hours_in_day", "hours > 0 AND hours <= 24");
        });

        builder.Property(e => e.Hours).HasPrecision(3, 1);

        builder.HasIndex(e => new { e.EmployeeId, e.Date, e.ProjectId }).IsUnique();
        builder.HasIndex(e => new { e.ProjectId, e.Date });

        // Deleting the project or the employee takes their hours with them.
        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
