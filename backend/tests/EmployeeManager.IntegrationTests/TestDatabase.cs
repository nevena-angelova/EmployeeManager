using EmployeeManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace EmployeeManager.IntegrationTests;

/// <summary>
/// A throwaway Postgres database with the real <see cref="EmployeeManagerDbContext"/> schema, dropped on dispose. It
/// uses the local dev server (docker compose), or the server in <c>EMPLOYEE_MANAGER_TEST_DB</c>; the role needs CREATEDB.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private const string DefaultServer = "Host=localhost;Port=5432;Username=employee_manager;Password=employee_manager";

    private readonly string serverConnectionString;
    private readonly string name = $"employee_manager_test_{Guid.NewGuid():N}";
    private readonly string connectionString;

    public TestDatabase()
    {
        serverConnectionString = new NpgsqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("EMPLOYEE_MANAGER_TEST_DB") ?? DefaultServer)
            { Database = "postgres", Pooling = false }
            .ConnectionString;
        connectionString = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = name }.ConnectionString;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public EmployeeManagerDbContext CreateContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<EmployeeManagerDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptors)
            .Options);

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
        using var connection = new NpgsqlConnection(serverConnectionString);
        connection.Open();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
        command.ExecuteNonQuery();
    }
}

/// <summary>Runs an action just before the first save, e.g. to make another request's change win a race.</summary>
public sealed class BeforeFirstSave(Func<Task> action) : SaveChangesInterceptor
{
    private bool done;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!done)
        {
            done = true;
            await action();
        }

        return result;
    }
}
