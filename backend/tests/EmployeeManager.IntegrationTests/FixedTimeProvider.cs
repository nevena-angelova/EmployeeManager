namespace EmployeeManager.IntegrationTests;

/// <summary>A clock stopped at <paramref name="now"/>.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    /// <summary>15 November 2026, so November 2026 is the current month.</summary>
    public static readonly FixedTimeProvider MidNovember2026 = new(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;
}
