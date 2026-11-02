using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasIndex(m => new { m.ProjectId, m.EmployeeId }).IsUnique();

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting an employee takes them off every project.
        builder.HasOne(m => m.Employee)
            .WithMany()
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
