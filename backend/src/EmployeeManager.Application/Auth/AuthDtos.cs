using System.ComponentModel.DataAnnotations;

namespace EmployeeManager.Application.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResultDto(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid EmployeeId,
    string Email,
    string FirstName,
    string LastName,
    string JobTitle,
    string Role);
