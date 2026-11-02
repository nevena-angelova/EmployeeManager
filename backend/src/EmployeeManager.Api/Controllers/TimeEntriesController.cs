using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using EmployeeManager.Application.TimeEntries;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.Api.Controllers;

/// <summary>
/// The hours the signed-in employee logs each day on their projects. <c>date</c> is <c>yyyy-MM-dd</c> and defaults
/// to today (UTC).
/// </summary>
[ApiController]
[Route("api/time-entries")]
public class TimeEntriesController(
    ITimeEntryService timeEntryService,
    IAuthService authService,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the signed-in employee's hours on the day on every project they're on or have hours on, with the
    /// totals for that week.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType<DayTimeEntriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DayTimeEntriesDto>> GetMine(string? date, CancellationToken cancellationToken)
    {
        if (ParseDate(date) is not { } parsed)
        {
            return InvalidDate();
        }

        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        return Ok(await timeEntryService.GetDayAsync(parsed, currentEmployeeId, cancellationToken));
    }

    /// <summary>
    /// Replaces the signed-in employee's hours on the day. Projects left out, or given 0 hours, are removed. Hours go
    /// in steps of 0.5, only on projects they're on, and at most 24 in total. Days after today give 409.
    /// </summary>
    [HttpPut("mine")]
    [ProducesResponseType<DayTimeEntriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DayTimeEntriesDto>> SaveMine(
        string? date,
        SaveDayTimeEntriesRequest request,
        CancellationToken cancellationToken)
    {
        if (ParseDate(date) is not { } parsed)
        {
            return InvalidDate();
        }

        if (await this.GetCurrentEmployeeIdAsync(authService, cancellationToken) is not { } currentEmployeeId)
        {
            return Unauthorized();
        }

        var result = await timeEntryService.SaveDayAsync(parsed, request, currentEmployeeId, cancellationToken);
        return result.Status == EmployeeResultStatus.Success ? Ok(result.Value) : this.ToErrorResponse(result);
    }

    /// <summary>The requested day, today if none was given, or null if it isn't <c>yyyy-MM-dd</c>.</summary>
    private DateOnly? ParseDate(string? date) =>
        string.IsNullOrEmpty(date) ? TimeEntryDay.Today(timeProvider)
        : TimeEntryDay.TryParse(date, out var parsed) ? parsed
        : null;

    private ActionResult InvalidDate() =>
        this.ToErrorResponse(EmployeeResult.Invalid("date", "The date must look like 2026-11-02."));
}
