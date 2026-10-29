using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", table =>
            table.HasCheckConstraint("ck_employees_manager_not_self", "manager_id IS NULL OR manager_id <> id"));

        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.JobTitle).HasMaxLength(150);

        builder.HasIndex(e => e.Email).IsUnique();

        // A manager with direct reports can't be deleted until they are reassigned.
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
