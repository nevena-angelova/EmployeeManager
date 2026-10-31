using EmployeeManager.Application.Employees.OrgChart;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrgChartService, OrgChartService>();
        return services;
    }
}
