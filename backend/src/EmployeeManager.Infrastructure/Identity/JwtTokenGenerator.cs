using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManager.Infrastructure.Identity;

public sealed class JwtTokenGenerator(JwtOptions options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler handler = new();

    public (string Token, DateTimeOffset ExpiresAt) Generate(ApplicationUser user, string role)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.ExpiryMinutes);

        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(options.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtOptions.RoleClaimType, role),
            ]),
        });

        return (token, expiresAt);
    }
}
