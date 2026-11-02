using EmployeeManager.Application.Common.Interfaces;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Application.TimeEntries;

public class TimeEntryService(IApplicationDbContext db, TimeProvider timeProvider) : ITimeEntryService
{
    private const string FutureDayError = "Hours can't be logged for days that haven't happened yet.";

    private const string ConcurrentChangeError =
        "Your hours or projects changed while you were saving. Reload the day and try again.";

    public async Task<DayTimeEntriesDto> GetDayAsync(
        DateOnly date,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.Members.Any(m => m.EmployeeId == employeeId)
                || db.TimeEntries.Any(e => e.ProjectId == p.Id && e.EmployeeId == employeeId && e.Date == date))
            .OrderBy(p => p.Name)
            .Select(p => new DayProjectHoursDto(
                p.Id,
                p.Name,
                p.Owner!.FirstName + " " + p.Owner.LastName,
                db.TimeEntries
                    .Where(e => e.ProjectId == p.Id && e.EmployeeId == employeeId && e.Date == date)
                    .Sum(e => (decimal?)e.Hours) ?? 0))
            .ToListAsync(cancellationToken);

        var weekStart = TimeEntryDay.WeekStart(date);
        var weekEnd = weekStart.AddDays(6);
        var weekTotals = await db.TimeEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId && e.Date >= weekStart && e.Date <= weekEnd)
            .GroupBy(e => e.Date)
            .Select(g => new { Date = g.Key, Hours = g.Sum(e => e.Hours) })
            .ToDictionaryAsync(g => g.Date, g => g.Hours, cancellationToken);

        return new DayTimeEntriesDto(
            TimeEntryDay.Format(date),
            TimeEntryDay.IsEditable(date, TimeEntryDay.Today(timeProvider)),
            TimeEntryRules.MaxHoursPerDay,
            projects,
            projects.Sum(p => p.Hours),
            Enumerable.Range(0, 7)
                .Select(weekStart.AddDays)
                .Select(day => new DayTotalDto(TimeEntryDay.Format(day), weekTotals.GetValueOrDefault(day)))
                .ToList());
    }

    public async Task<EmployeeResult<DayTimeEntriesDto>> SaveDayAsync(
        DateOnly date,
        SaveDayTimeEntriesRequest request,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        if (!TimeEntryDay.IsEditable(date, TimeEntryDay.Today(timeProvider)))
        {
            return EmployeeResult<DayTimeEntriesDto>.From(EmployeeResult.Conflict(FutureDayError));
        }

        var items = request.Entries ?? [];
        var existing = await db.TimeEntries
            .Where(e => e.EmployeeId == employeeId && e.Date == date)
            .ToListAsync(cancellationToken);
        var memberOf = await db.ProjectMembers
            .Where(m => m.EmployeeId == employeeId)
            .Select(m => m.ProjectId)
            .ToListAsync(cancellationToken);

        // Hours already logged on a project they've since left can stay or change, as GetDayAsync offers.
        var allowed = memberOf.Concat(existing.Select(e => e.ProjectId)).ToHashSet();
        if (TimeEntryRules.Validate(items, allowed) is { } error)
        {
            return EmployeeResult<DayTimeEntriesDto>.From(EmployeeResult.Invalid("entries", error));
        }

        var wanted = items.Where(e => e.Hours > 0).ToDictionary(e => e.ProjectId, e => e.Hours);
        foreach (var entry in existing)
        {
            if (wanted.Remove(entry.ProjectId, out var hours))
            {
                entry.Hours = hours;
            }
            else
            {
                db.TimeEntries.Remove(entry);
            }
        }

        foreach (var (projectId, hours) in wanted)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                Date = date,
                Hours = hours,
            });
        }

        // Fails on another save of the same day (the unique index or a row already removed), or a project being
        // deleted meanwhile.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return EmployeeResult<DayTimeEntriesDto>.From(EmployeeResult.Conflict(ConcurrentChangeError));
        }

        return EmployeeResult<DayTimeEntriesDto>.Success(await GetDayAsync(date, employeeId, cancellationToken));
    }
}
