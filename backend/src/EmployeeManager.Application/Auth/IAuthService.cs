namespace EmployeeManager.Application.Auth;

public interface IAuthService
{
    /// <summary>Returns an access token for valid credentials, or null if they are wrong or the account is locked out.</summary>
    Task<LoginResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Returns the signed-in user, or null if the user no longer exists.</summary>
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
