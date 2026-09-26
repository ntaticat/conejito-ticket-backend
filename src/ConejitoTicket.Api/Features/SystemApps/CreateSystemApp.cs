using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ConejitoTicket.Api.Features.SystemApps;

public static class CreateSystemApp
{
    public record CreateSystemAppRequest([Required, MaxLength(150)] string Name);

    // Única respuesta que incluye el secret: en BD solo queda su hash.
    public record SystemAppCredentialsDto(Guid Id, string Name, string ClientId, string ClientSecret);

    internal static string NewClientId() => $"ct_{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8))}";

    internal static string NewClientSecret() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public sealed class Handler(AppDbContext context)
    {
        public async Task<SystemAppCredentialsDto> HandleAsync(CreateSystemAppRequest request, CancellationToken cancellationToken)
        {
            var secret = NewClientSecret();
            var app = new SystemApp
            {
                Name = request.Name.Trim(),
                ClientId = NewClientId(),
                ClientSecretHash = Hasher.Hash(secret),
            };

            context.SystemApps.Add(app);
            await context.SaveChangesAsync(cancellationToken);

            return new SystemAppCredentialsDto(app.Id, app.Name, app.ClientId, secret);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/system-apps", async (
                    [FromBody] CreateSystemAppRequest request,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(request, cancellationToken);
                    return Results.Created($"/api/v1/system-apps/{result.Id}", result);
                })
                .RequireAuthorization(Roles.Admin)
                .WithTags("SystemApps");
        }
    }
}
