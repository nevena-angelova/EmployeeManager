using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Infrastructure.Employees;
using EmployeeManager.Infrastructure.Identity;
using EmployeeManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, JwtOptions jwtOptions)
    {
        services.AddDbContext<EmployeeManagerDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(EmployeeManagerDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<EmployeeManagerDbContext>());

        // Identity Core only: user/role stores and password hashing, no cookie sign-in. The API authenticates with JWTs.
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EmployeeManagerDbContext>();

        jwtOptions.Validate();
        services.AddSingleton(jwtOptions);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeManagementService, EmployeeManagementService>();

        return services;
    }
}
