using System.ComponentModel.DataAnnotations;

namespace EmployeeManager.Application.Employees.Management;

public sealed record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    DateOnly HireDate,
    Guid? ManagerId,
    string? ManagerName,
    string Role);

/// <summary>Creates an employee together with their sign-in account.</summary>
public sealed record CreateEmployeeRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(150)] string JobTitle,
    DateOnly HireDate,
    Guid? ManagerId,
    [Required] string Role,
    [Required] string Password);

/// <summary>Updates an employee's details, manager and role. The password is not changed here.</summary>
public sealed record UpdateEmployeeRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(150)] string JobTitle,
    DateOnly HireDate,
    Guid? ManagerId,
    [Required] string Role);
