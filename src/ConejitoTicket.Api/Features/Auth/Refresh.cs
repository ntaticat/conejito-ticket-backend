using ConejitoTicket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Auth;

public static class Refresh
{
    // Dos pestañas pueden renovar a la vez con la misma cookie: la segunda llega con un token recién revocado.
    // Dentro de esta ventana se trata como concurrencia legítima y no como robo.
    private static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

    public sealed class Handler(AppDbContext context, TokenIssuer tokens, RefreshTokens refreshTokens)
    {
        public async Task<Login.LoginResponse?> HandleAsync(HttpContext http, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var current = await refreshTokens.FindAsync(http.Request, cancellationToken);

            if (current is null || current.ExpiresAtUtc <= now)
                return Reject(http);

            if (current.RevokedAtUtc is { } revokedAt)
            {
                // Concurrencia legítima solo si la familia sigue viva: tras un logout o un reuso ya no queda token activo.
                var concurrent = revokedAt >= now - ReuseGrace
                                 && await refreshTokens.IsFamilyActiveAsync(current.FamilyId, now, cancellationToken);
                if (!concurrent)
                {
                    // Reuso de un token ya rotado: posible robo, se corta toda la sesión.
                    await refreshTokens.RevokeFamilyAsync(current.FamilyId, cancellationToken);
                    return Reject(http);
                }
            }

            var user = await context.AdminUsers.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == current.AdminUserId, cancellationToken);
            if (user is null) return Reject(http);

            current.RevokedAtUtc ??= now;
            refreshTokens.Issue(http.Response, user.Id, current.FamilyId);
            await context.SaveChangesAsync(cancellationToken);

            var (token, expires) = tokens.Issue(user.Id, user.UserName, Roles.Admin);
            return new Login.LoginResponse(token, expires);
        }

        private static Login.LoginResponse? Reject(HttpContext http)
        {
            RefreshTokens.ClearCookie(http.Response);
            return null;
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/auth/refresh", async (HttpContext http, Handler handler, CancellationToken cancellationToken) =>
                    await handler.HandleAsync(http, cancellationToken) is { } result
                        ? Results.Ok(result)
                        : Results.Unauthorized())
                .AllowAnonymous()
                .WithTags("Auth");
        }
    }
}
