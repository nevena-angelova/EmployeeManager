using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManager.Infrastructure.Identity;

/// <summary>Settings for issuing and validating access tokens, bound from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The claim that carries the user's role. Tokens use short JWT claim names rather than the long .NET URIs.</summary>
    public const string RoleClaimType = "role";

    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int ExpiryMinutes { get; init; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    /// <summary>Throws if a required setting is missing, so a misconfigured API fails at startup rather than on first sign-in.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("'Jwt:Issuer' and 'Jwt:Audience' must be configured.");
        }

        // HMAC-SHA256 needs a key of at least 256 bits.
        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException("'Jwt:SigningKey' must be configured and at least 32 bytes long.");
        }

        if (ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("'Jwt:ExpiryMinutes' must be positive.");
        }
    }
}
