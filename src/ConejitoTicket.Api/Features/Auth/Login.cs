using System.ComponentModel.DataAnnotations;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Auth;

public static class Login
{
    public record LoginRequest([Required, MaxLength(100)] string UserName, [Required, MaxLength(200)] string Password);

    public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

    public sealed class Handler(AppDbContext context, TokenIssuer tokens, RefreshTokens refreshTokens)
    {
        // Hash señuelo para que un usuario inexistente tarde lo mismo que una contraseña incorrecta.
        private static readonly string DummyHash = Hasher.Hash(Guid.NewGuid().ToString());

        public async Task<LoginResponse?> HandleAsync(
            LoginRequest request, HttpResponse response, CancellationToken cancellationToken)
        {
            var user = await context.AdminUsers.AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

            if (!Hasher.Verify(request.Password, user?.PasswordHash ?? DummyHash) || user is null)
                return null;

            refreshTokens.Issue(response, user.Id, familyId: Guid.CreateVersion7());
            await context.SaveChangesAsync(cancellationToken);

            var (token, expires) = tokens.Issue(user.Id, user.UserName, Roles.Admin);
            return new LoginResponse(token, expires);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/auth/login", async (
                    [FromBody] LoginRequest request,
                    HttpResponse response,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(request, response, cancellationToken);
                    return result is null ? Results.Unauthorized() : Results.Ok(result);
                })
                .AllowAnonymous()
                .WithTags("Auth");
        }
    }
}
