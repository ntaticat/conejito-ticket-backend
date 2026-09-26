using System.Text.Json.Serialization;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Auth;

// OAuth2 Client Credentials (RFC 6749 §4.4): form-urlencoded, respuesta en snake_case.
public static class ClientToken
{
    public record ClientTokenRequest(string? GrantType, string? ClientId, string? ClientSecret);

    public record ClientTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    public record OAuthError([property: JsonPropertyName("error")] string Error);

    public sealed class Handler(AppDbContext context, TokenIssuer tokens)
    {
        private static readonly string DummyHash = Hasher.Hash(Guid.NewGuid().ToString());

        public async Task<IResult> HandleAsync(ClientTokenRequest request, CancellationToken cancellationToken)
        {
            if (request.GrantType != "client_credentials")
                return Results.BadRequest(new OAuthError("unsupported_grant_type"));

            if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
                return Results.BadRequest(new OAuthError("invalid_request"));

            var app = await context.SystemApps.AsNoTracking()
                .SingleOrDefaultAsync(s => s.ClientId == request.ClientId, cancellationToken);

            if (!Hasher.Verify(request.ClientSecret, app?.ClientSecretHash ?? DummyHash) || app is not { IsActive: true })
                return Results.Json(new OAuthError("invalid_client"), statusCode: StatusCodes.Status401Unauthorized);

            var (token, expires) = tokens.Issue(app.Id, app.Name, Roles.System);
            var expiresIn = (int)(expires - DateTime.UtcNow).TotalSeconds;
            return Results.Ok(new ClientTokenResponse(token, "Bearer", expiresIn));
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/auth/connect/token", (
                    [FromForm(Name = "grant_type")] string? grantType,
                    [FromForm(Name = "client_id")] string? clientId,
                    [FromForm(Name = "client_secret")] string? clientSecret,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                    handler.HandleAsync(new ClientTokenRequest(grantType, clientId, clientSecret), cancellationToken))
                .AllowAnonymous()
                .DisableAntiforgery()
                .WithTags("Auth");
        }
    }
}
