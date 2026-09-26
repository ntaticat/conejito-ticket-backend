using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using ConejitoTicket.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConejitoTicket.Api.Infrastructure;

// Refresh token opaco en cookie HttpOnly; en BD solo se guarda su SHA-256.
// ponytail: las filas vencidas o revocadas se acumulan; purga periódica cuando la tabla crezca.
public sealed class RefreshTokens(AppDbContext context, IOptions<JwtOptions> options)
{
    private const string CookieName = "rt";
    private const string CookiePath = "/api/v1/auth";

    // Se agrega al contexto; el llamador hace SaveChanges.
    public void Issue(HttpResponse response, Guid adminUserId, Guid familyId)
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var expires = DateTime.UtcNow.AddDays(options.Value.RefreshDays);

        context.RefreshTokens.Add(new RefreshToken
        {
            AdminUserId = adminUserId,
            FamilyId = familyId,
            TokenHash = Hash(raw),
            ExpiresAtUtc = expires,
        });

        response.Cookies.Append(CookieName, raw, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expires,
        });
    }

    public async Task<RefreshToken?> FindAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrEmpty(raw)) return null;
        var hash = Hash(raw);
        return await context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public Task<bool> IsFamilyActiveAsync(Guid familyId, DateTime now, CancellationToken cancellationToken) =>
        context.RefreshTokens.AnyAsync(
            t => t.FamilyId == familyId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now, cancellationToken);

    public Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now), cancellationToken);
    }

    public static void ClearCookie(HttpResponse response) =>
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = CookiePath,
        });

    private static string Hash(string raw) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
