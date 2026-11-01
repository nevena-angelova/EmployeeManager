namespace EmployeeManager.Application.Employees.Management;

public enum EmployeeResultStatus
{
    Success,
    NotFound,
    /// <summary>The request breaks a rule tied to one of its fields; see <see cref="EmployeeResult.Errors"/>.</summary>
    Invalid,
    /// <summary>The request is valid but can't be applied in the current state; see <see cref="EmployeeResult.Detail"/>.</summary>
    Conflict,
}

/// <summary>The outcome of an employee command, which the API maps to a status code.</summary>
public class EmployeeResult
{
    protected EmployeeResult(EmployeeResultStatus status, IReadOnlyDictionary<string, string[]>? errors = null, string? detail = null)
    {
        Status = status;
        Errors = errors ?? new Dictionary<string, string[]>();
        Detail = detail;
    }

    public EmployeeResultStatus Status { get; }

    /// <summary>Error messages keyed by the camelCase request field they belong to.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public string? Detail { get; }

    public static EmployeeResult Success() => new(EmployeeResultStatus.Success);
    public static EmployeeResult NotFound() => new(EmployeeResultStatus.NotFound);
    public static EmployeeResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(EmployeeResultStatus.Invalid, errors);
    public static EmployeeResult Invalid(string field, string error) => Invalid(new Dictionary<string, string[]> { [field] = [error] });
    public static EmployeeResult Conflict(string detail) => new(EmployeeResultStatus.Conflict, detail: detail);
}

public sealed class EmployeeResult<T> : EmployeeResult
{
    private EmployeeResult(EmployeeResultStatus status, T? value, IReadOnlyDictionary<string, string[]>? errors, string? detail)
        : base(status, errors, detail) => Value = value;

    /// <summary>Set when <see cref="EmployeeResult.Status"/> is <see cref="EmployeeResultStatus.Success"/>.</summary>
    public T? Value { get; }

    public static EmployeeResult<T> Success(T value) => new(EmployeeResultStatus.Success, value, null, null);

    /// <summary>Carries a failed result over to a different value type.</summary>
    public static EmployeeResult<T> From(EmployeeResult failure) =>
        new(failure.Status, default, failure.Errors, failure.Detail);
}
