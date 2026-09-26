using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.Tickets;

public static class GetTickets
{
    public record GetTicketsRequest(
        TicketStatus? Status = null,
        TicketPriority? Priority = null,
        Guid? SystemAppId = null,
        string? TenantId = null,
        int Page = 1,
        int PageSize = 20);

    public record TicketDto(
        Guid Id,
        long TicketNumber,
        Guid SystemAppId,
        string SystemAppName,
        string? TenantId,
        string? TenantName,
        string? ReporterUserName,
        string? ReporterUserEmail,
        string Title,
        TicketType Type,
        TicketPriority Priority,
        TicketStatus Status,
        DateTime CreatedAtUtc,
        DateTime? ResolvedAtUtc);

    public record PagedResponse(IReadOnlyList<TicketDto> Items, int Page, int PageSize, int TotalCount);

    public sealed class Handler(AppDbContext context)
    {
        public async Task<PagedResponse> HandleAsync(GetTicketsRequest request, CancellationToken cancellationToken)
        {
            // [Range] en parámetros de [AsParameters] rompe la generación de OpenAPI en .NET 10; se acota aquí.
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = context.Tickets.AsNoTracking();

            if (request.Status is { } status) query = query.Where(t => t.Status == status);
            if (request.Priority is { } priority) query = query.Where(t => t.Priority == priority);
            if (request.SystemAppId is { } appId) query = query.Where(t => t.SystemAppId == appId);
            if (!string.IsNullOrWhiteSpace(request.TenantId)) query = query.Where(t => t.TenantId == request.TenantId);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(t => t.TicketNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TicketDto(
                    t.Id, t.TicketNumber, t.SystemAppId, t.SystemApp!.Name, t.TenantId, t.TenantName,
                    t.ReporterUserName, t.ReporterUserEmail, t.Title, t.Type, t.Priority, t.Status,
                    t.CreatedAtUtc, t.ResolvedAtUtc))
                .ToListAsync(cancellationToken);

            return new PagedResponse(items, page, pageSize, total);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("api/v1/tickets", async (
                    [AsParameters] GetTicketsRequest request,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await handler.HandleAsync(request, cancellationToken)))
                .RequireAuthorization(Roles.Admin)
                .WithTags("Tickets");
        }
    }
}
