using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ConejitoTicket.Api.Features.Tickets;

public static class UpdateTicketStatus
{
    public record UpdateTicketStatusRequest(TicketStatus Status);

    public sealed class Handler(AppDbContext context)
    {
        public async Task<bool> HandleAsync(Guid id, TicketStatus status, CancellationToken cancellationToken)
        {
            var ticket = await context.Tickets.FindAsync([id], cancellationToken);
            if (ticket is null) return false;

            ticket.Status = status;
            // Se conserva la fecha de resolución al cerrar; se limpia si el ticket se reabre.
            ticket.ResolvedAtUtc = status is TicketStatus.Resolved or TicketStatus.Closed
                ? ticket.ResolvedAtUtc ?? DateTime.UtcNow
                : null;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/v1/tickets/{id:guid}/status", async (
                    Guid id,
                    [FromBody] UpdateTicketStatusRequest request,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                    await handler.HandleAsync(id, request.Status, cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
                .RequireAuthorization(Roles.Admin)
                .WithTags("Tickets");
        }
    }
}
