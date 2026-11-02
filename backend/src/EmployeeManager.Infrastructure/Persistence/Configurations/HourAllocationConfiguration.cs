using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class HourAllocationConfiguration : IEntityTypeConfiguration<HourAllocation>
{
    public void Configure(EntityTypeBuilder<HourAllocation> builder)
    {
        builder.ToTable("hour_allocations", table =>
        {
            table.HasCheckConstraint("ck_hour_allocations_hours_positive", "hours > 0");
            table.HasCheckConstraint("ck_hour_allocations_month_first_day", "extract(day from month) = 1");
        });

        builder.Property(a => a.Hours).HasPrecision(5, 1);

        builder.HasIndex(a => new { a.EmployeeId, a.ProjectId, a.Month }).IsUnique();
        builder.HasIndex(a => new { a.ProjectId, a.Month });

        // Deleting the project or the employee takes their hours with them.
        builder.HasOne(a => a.Project)
            .WithMany()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
