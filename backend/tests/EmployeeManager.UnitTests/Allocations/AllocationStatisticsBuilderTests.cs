using EmployeeManager.Application.Allocations;

namespace EmployeeManager.UnitTests.Allocations;

public class AllocationStatisticsBuilderTests
{
    private static readonly AllocationProjectDto Alpha = new(Guid.NewGuid(), "Alpha");
    private static readonly AllocationProjectDto Beta = new(Guid.NewGuid(), "Beta");
    private static readonly Guid Elsewhere = Guid.NewGuid();
    private static readonly StatisticsMember Oscar = new(Guid.NewGuid(), "Oscar", "Report", "Engineer");
    private static readonly StatisticsMember Rachel = new(Guid.NewGuid(), "Rachel", "Report", "Engineer");

    [Fact]
    public void Build_WithNoHours_ReturnsTwelveEmptyMonths()
    {
        var statistics = Build([], []);

        Assert.Equal(2026, statistics.Year);
        Assert.Equal(Enumerable.Range(1, 12).Select(m => $"2026-{m:00}"), statistics.Months.Select(m => m.Month));
        Assert.All(statistics.Months, m => Assert.Equal((0m, 0m, 0m, 0m), (m.Planned, m.Logged, m.OtherPlanned, m.OtherLogged)));
        Assert.All(statistics.Projects, p => Assert.Equal(12, p.Months.Count));
        Assert.All(statistics.Members, m => Assert.Equal(12, m.Months.Count));
        Assert.Equal((0m, 0m), (statistics.TotalPlanned, statistics.TotalLogged));
    }

    [Fact]
    public void Build_UsesEachMonthsCapacity()
    {
        var statistics = Build([], []);

        Assert.Equal(160, statistics.Months[1].CapacityHours);
        Assert.Equal(168, statistics.Months[10].CapacityHours);
    }

    [Fact]
    public void Build_KeepsTheGivenOrder()
    {
        var statistics = Build([], []);

        Assert.Equal(["Alpha", "Beta"], statistics.Projects.Select(p => p.Name));
        Assert.Equal([Oscar.EmployeeId, Rachel.EmployeeId], statistics.Members.Select(m => m.EmployeeId));
    }

    [Fact]
    public void Build_SumsByMonthProjectAndReport()
    {
        var statistics = Build(
            [
                Own(Rachel, Alpha, 11, 40),
                Own(Rachel, Beta, 11, 20),
                Own(Oscar, Alpha, 11, 10),
                Own(Rachel, Alpha, 12, 30),
            ],
            [
                Own(Rachel, Alpha, 11, 35),
                Own(Oscar, Alpha, 10, 8),
            ]);

        Assert.Equal((70m, 35m), (statistics.Months[10].Planned, statistics.Months[10].Logged));
        Assert.Equal((0m, 8m), (statistics.Months[9].Planned, statistics.Months[9].Logged));
        Assert.Equal((30m, 0m), (statistics.Months[11].Planned, statistics.Months[11].Logged));

        var alpha = statistics.Projects[0];
        Assert.Equal(new MonthHoursDto("2026-11", 50, 35), alpha.Months[10]);
        Assert.Equal((80m, 43m), (alpha.TotalPlanned, alpha.TotalLogged));
        Assert.Equal((20m, 0m), (statistics.Projects[1].TotalPlanned, statistics.Projects[1].TotalLogged));

        var rachel = statistics.Members[1];
        Assert.Equal(new MonthHoursDto("2026-11", 60, 35), rachel.Months[10]);
        Assert.Equal((90m, 35m), (rachel.TotalPlanned, rachel.TotalLogged));

        Assert.Equal((100m, 43m), (statistics.TotalPlanned, statistics.TotalLogged));
    }

    [Fact]
    public void Build_PutsOtherManagersProjectsOnlyInTheOtherHours()
    {
        var statistics = Build(
            [new StatisticsHours(Rachel.EmployeeId, Elsewhere, new DateOnly(2026, 11, 1), 25, IsOwn: false)],
            [new StatisticsHours(Rachel.EmployeeId, Elsewhere, new DateOnly(2026, 11, 1), 12, IsOwn: false)]);

        var november = statistics.Months[10];
        Assert.Equal((0m, 0m, 25m, 12m), (november.Planned, november.Logged, november.OtherPlanned, november.OtherLogged));
        Assert.Equal(0, statistics.Members[1].TotalPlanned);
        Assert.Equal(0, statistics.TotalPlanned);
    }

    [Fact]
    public void Build_IgnoresOtherYearsAndPeopleWhoAreNotReports()
    {
        var stranger = new StatisticsMember(Guid.NewGuid(), "Sam", "Stranger", "Engineer");

        var statistics = Build(
            [
                new StatisticsHours(Rachel.EmployeeId, Alpha.Id, new DateOnly(2025, 11, 1), 40, IsOwn: true),
                Own(stranger, Alpha, 11, 40),
            ],
            []);

        Assert.Equal(0, statistics.TotalPlanned);
    }

    private static TeamStatisticsDto Build(StatisticsHours[] planned, StatisticsHours[] logged) =>
        AllocationStatisticsBuilder.Build(2026, [Alpha, Beta], [Oscar, Rachel], planned, logged);

    private static StatisticsHours Own(StatisticsMember member, AllocationProjectDto project, int month, decimal hours) =>
        new(member.EmployeeId, project.Id, new DateOnly(2026, month, 1), hours, IsOwn: true);
}
