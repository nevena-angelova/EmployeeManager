using EmployeeManager.Application;
using EmployeeManager.Infrastructure;
using EmployeeManager.Infrastructure.Persistence;
using EmployeeManager.Infrastructure.Persistence.Seeding;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EmployeeManagerDb")
    ?? throw new InvalidOperationException("Connection string 'EmployeeManagerDb' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EmployeeManagerDbContext>();

builder.Services.AddCors(options =>
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<EmployeeManagerDbContext>());
}

app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();
