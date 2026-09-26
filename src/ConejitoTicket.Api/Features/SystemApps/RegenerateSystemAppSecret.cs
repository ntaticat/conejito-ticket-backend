using ConejitoTicket.Api.Infrastructure;

namespace ConejitoTicket.Api.Features.SystemApps;

public static class RegenerateSystemAppSecret
{
    public record SecretDto(string ClientId, string ClientSecret);

    public sealed class Handler(AppDbContext context)
    {
        // El secret anterior deja de servir en cuanto se guarda el nuevo hash.
        public async Task<SecretDto?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var app = await context.SystemApps.FindAsync([id], cancellationToken);
            if (app is null) return null;

            var secret = CreateSystemApp.NewClientSecret();
            app.ClientSecretHash = Hasher.Hash(secret);
            await context.SaveChangesAsync(cancellationToken);

            return new SecretDto(app.ClientId, secret);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/system-apps/{id:guid}/secret", async (Guid id, Handler handler, CancellationToken cancellationToken) =>
                    await handler.HandleAsync(id, cancellationToken) is { } result
                        ? Results.Ok(result)
                        : Results.NotFound())
                .RequireAuthorization(Roles.Admin)
                .WithTags("SystemApps");
        }
    }
}
