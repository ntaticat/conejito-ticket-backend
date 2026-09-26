using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Tickets;

public static class GetTicket
{
    public record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes);

    public record TicketDetailDto(
        Guid Id,
        long TicketNumber,
        Guid SystemAppId,
        string SystemAppName,
        string? TenantId,
        string? TenantName,
        string? ReporterUserId,
        string? ReporterUserName,
        string? ReporterUserEmail,
        string Title,
        string Description,
        TicketType Type,
        TicketPriority Priority,
        TicketStatus Status,
        string? MetadataJson,
        DateTime CreatedAtUtc,
        DateTime? ResolvedAtUtc,
        IReadOnlyList<AttachmentDto> Attachments);

    public sealed class Handler(AppDbContext context)
    {
        public Task<TicketDetailDto?> HandleAsync(Guid id, CancellationToken cancellationToken) =>
            context.Tickets.AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TicketDetailDto(
                    t.Id, t.TicketNumber, t.SystemAppId, t.SystemApp!.Name, t.TenantId, t.TenantName,
                    t.ReporterUserId, t.ReporterUserName, t.ReporterUserEmail, t.Title, t.Description,
                    t.Type, t.Priority, t.Status, t.MetadataJson, t.CreatedAtUtc, t.ResolvedAtUtc,
                    t.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes)).ToList()))
                .SingleOrDefaultAsync(cancellationToken);
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("api/v1/tickets/{id:guid}", async (Guid id, Handler handler, CancellationToken cancellationToken) =>
                    await handler.HandleAsync(id, cancellationToken) is { } ticket
                        ? Results.Ok(ticket)
                        : Results.NotFound())
                .RequireAuthorization(Roles.Admin)
                .WithTags("Tickets");
        }
    }
}
