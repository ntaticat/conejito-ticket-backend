using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConejitoTicket.Api.Infrastructure;

public static class Roles
{
    public const string Admin = "Admin";
    public const string System = "System";
}

public sealed class JwtOptions
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int ExpiresMinutes { get; init; } = 15;
    public int RefreshDays { get; init; } = 14;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}

public sealed class TokenIssuer(IOptions<JwtOptions> options)
{
    public (string Token, DateTime ExpiresAtUtc) Issue(Guid subject, string name, string role)
    {
        var jwt = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, subject.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim("role", role),
            ]),
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
        });

        return (token, expires);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetSubjectId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? throw new UnauthorizedAccessException("Token sin 'sub'."));
}
