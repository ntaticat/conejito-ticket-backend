using ConejitoTicket.Api.Infrastructure;

namespace ConejitoTicket.Api.Features.Auth;

public static class Logout
{
    public sealed class Handler(RefreshTokens refreshTokens)
    {
        public async Task HandleAsync(HttpContext http, CancellationToken cancellationToken)
        {
            if (await refreshTokens.FindAsync(http.Request, cancellationToken) is { } current)
                await refreshTokens.RevokeFamilyAsync(current.FamilyId, cancellationToken);

            RefreshTokens.ClearCookie(http.Response);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            // Anónimo: el access token puede haber expirado; la cookie identifica la sesión.
            app.MapPost("api/v1/auth/logout", async (HttpContext http, Handler handler, CancellationToken cancellationToken) =>
                {
                    await handler.HandleAsync(http, cancellationToken);
                    return Results.NoContent();
                })
                .AllowAnonymous()
                .WithTags("Auth");
        }
    }
}
