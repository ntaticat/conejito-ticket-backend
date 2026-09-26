using ConejitoTicket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Tickets;

public static class GetAttachment
{
    public sealed class Handler(AppDbContext context)
    {
        public async Task<IResult> HandleAsync(Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
        {
            var attachment = await context.TicketAttachments.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId, cancellationToken);

            var path = attachment is null ? null : Path.GetFullPath(attachment.StoragePath);
            if (attachment is null || !File.Exists(path)) return Results.NotFound();

            return Results.File(path, attachment.ContentType, attachment.FileName);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("api/v1/tickets/{ticketId:guid}/attachments/{attachmentId:guid}", (
                    Guid ticketId, Guid attachmentId, Handler handler, CancellationToken cancellationToken) =>
                    handler.HandleAsync(ticketId, attachmentId, cancellationToken))
                .RequireAuthorization(Roles.Admin)
                .WithTags("Tickets");
        }
    }
}
