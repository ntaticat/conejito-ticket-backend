using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ConejitoTicket.Api.Features.Tickets;

public static class CreateTicket
{
    public record CreateTicketRequest(
        [Required, MaxLength(200)] string Title,
        [Required, MaxLength(10_000)] string Description,
        TicketType Type,
        TicketPriority Priority = TicketPriority.Medium,
        [MaxLength(100)] string? TenantId = null,
        [MaxLength(200)] string? TenantName = null,
        [MaxLength(100)] string? ReporterUserId = null,
        [MaxLength(200)] string? ReporterUserName = null,
        [MaxLength(200), EmailAddress] string? ReporterUserEmail = null,
        JsonElement? Metadata = null);

    public record TicketCreatedDto(Guid Id, long TicketNumber, TicketStatus Status, DateTime CreatedAtUtc);

    public sealed class Handler(AppDbContext context)
    {
        public async Task<TicketCreatedDto> HandleAsync(
            CreateTicketRequest request, Guid systemAppId, CancellationToken cancellationToken)
        {
            var metadata = request.Metadata switch
            {
                null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
                { ValueKind: JsonValueKind.Object } m => m.GetRawText(),
                _ => throw new ArgumentException("Metadata debe ser un objeto JSON."),
            };

            var ticket = new Ticket
            {
                SystemAppId = systemAppId,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Priority = request.Priority,
                TenantId = request.TenantId,
                TenantName = request.TenantName,
                ReporterUserId = request.ReporterUserId,
                ReporterUserName = request.ReporterUserName,
                ReporterUserEmail = request.ReporterUserEmail,
                MetadataJson = metadata,
            };

            await context.Tickets.AddAsync(ticket, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new TicketCreatedDto(ticket.Id, ticket.TicketNumber, ticket.Status, ticket.CreatedAtUtc);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/tickets", async (
                    [FromBody] CreateTicketRequest request,
                    ClaimsPrincipal user,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(request, user.GetSubjectId(), cancellationToken);
                    return Results.Created($"/api/v1/tickets/{result.Id}", result);
                })
                .RequireAuthorization(Roles.System)
                .WithTags("Tickets");
        }
    }
}
