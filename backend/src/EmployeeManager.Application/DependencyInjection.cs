using EmployeeManager.Application.Allocations;
using EmployeeManager.Application.Employees.OrgChart;
using EmployeeManager.Application.Projects;
using EmployeeManager.Application.TimeEntries;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrgChartService, OrgChartService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<ITimeEntryService, TimeEntryService>();
        return services;
    }
}
