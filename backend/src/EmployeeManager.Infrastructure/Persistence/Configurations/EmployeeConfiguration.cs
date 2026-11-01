using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    // Named explicitly because EmployeeManagementService recognises violations of them by name.
    public const string EmailIndexName = "ix_employees_email";
    public const string ManagerForeignKeyName = "fk_employees_employees_manager_id";

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", table =>
            table.HasCheckConstraint("ck_employees_manager_not_self", "manager_id IS NULL OR manager_id <> id"));

        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.JobTitle).HasMaxLength(150);

        // Case-insensitive, so the unique index rejects "Ann@x.com" next to "ann@x.com" and lookups can use it.
        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .UseCollation(EmployeeManagerDbContext.CaseInsensitiveCollation);

        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName(EmailIndexName);

        // A manager with direct reports can't be deleted until they are reassigned.
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .HasConstraintName(ManagerForeignKeyName)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
