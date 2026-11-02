using System.Security.Claims;
using EmployeeManager.Application.Auth;
using EmployeeManager.Application.Employees.Management;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EmployeeManager.Api.Controllers;

/// <summary>Helpers shared by the controllers.</summary>
internal static class ControllerExtensions
{
    /// <summary>The signed-in user's employee id, or null if their account no longer exists.</summary>
    public static async Task<Guid?> GetCurrentEmployeeIdAsync(
        this ControllerBase controller,
        IAuthService authService,
        CancellationToken cancellationToken) =>
        Guid.TryParse(controller.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? (await authService.GetCurrentUserAsync(userId, cancellationToken))?.EmployeeId
            : null;

    /// <summary>Maps a failed result to 404, 409 <see cref="ProblemDetails"/> or 400 <see cref="ValidationProblemDetails"/>.</summary>
    public static ActionResult ToErrorResponse(this ControllerBase controller, EmployeeResult result)
    {
        switch (result.Status)
        {
            case EmployeeResultStatus.NotFound:
                return controller.NotFound();
            case EmployeeResultStatus.Conflict:
                return controller.Problem(detail: result.Detail, statusCode: StatusCodes.Status409Conflict);
            default:
                foreach (var (field, messages) in result.Errors)
                {
                    foreach (var message in messages)
                    {
                        controller.ModelState.AddModelError(field, message);
                    }
                }

                return controller.ValidationProblem(controller.ModelState);
        }
    }
}
