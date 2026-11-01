using EmployeeManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManager.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        // One account per employee; deleting the employee deletes their account.
        builder.HasOne(u => u.Employee)
            .WithOne()
            .HasForeignKey<ApplicationUser>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
