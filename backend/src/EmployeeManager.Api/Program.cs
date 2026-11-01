using System.Text.Json;
using EmployeeManager.Api;
using EmployeeManager.Application;
using EmployeeManager.Application.Auth;
using EmployeeManager.Infrastructure;
using EmployeeManager.Infrastructure.Identity;
using EmployeeManager.Infrastructure.Persistence;
using EmployeeManager.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EmployeeManagerDb")
    ?? throw new InvalidOperationException("Connection string 'EmployeeManagerDb' is not configured.");

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, jwtOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the token's short claim names ("sub", "role") instead of mapping them to the long .NET claim URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.CreateSigningKey(),
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = JwtOptions.RoleClaimType,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Every endpoint requires a signed-in user unless it opts out with [AllowAnonymous] / AllowAnonymous().
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(AuthorizationPolicies.ManagerOnly, policy => policy.RequireRole(Roles.Manager))
    .AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));

// Camel-case dictionary keys too, so validation errors are keyed by the JSON property name ("email", not "Email").
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase);
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
    app.MapOpenApi().AllowAnonymous();

    using var scope = app.Services.CreateScope();
    await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

app.Run();
