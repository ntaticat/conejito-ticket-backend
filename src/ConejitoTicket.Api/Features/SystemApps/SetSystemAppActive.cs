using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ConejitoTicket.Api.Features.SystemApps;

// Borrado lógico reversible: la fila se conserva para los tickets históricos.
public static class SetSystemAppActive
{
    public record SetSystemAppActiveRequest(bool IsActive);

    public sealed class Handler(AppDbContext context)
    {
        public async Task<bool> HandleAsync(Guid id, bool isActive, CancellationToken cancellationToken)
        {
            var app = await context.SystemApps.FindAsync([id], cancellationToken);
            if (app is null) return false;

            app.IsActive = isActive;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/v1/system-apps/{id:guid}", async (
                    Guid id,
                    [FromBody] SetSystemAppActiveRequest request,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                    await handler.HandleAsync(id, request.IsActive, cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
                .RequireAuthorization(Roles.Admin)
                .WithTags("SystemApps");
        }
    }
}
