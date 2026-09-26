using ConejitoTicket.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.SystemApps;

public static class GetSystemApps
{
    public record SystemAppDto(Guid Id, string Name, string ClientId, bool IsActive, DateTime CreatedAtUtc);

    public sealed class Handler(AppDbContext context)
    {
        public Task<List<SystemAppDto>> HandleAsync(CancellationToken cancellationToken) =>
            context.SystemApps.AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new SystemAppDto(s.Id, s.Name, s.ClientId, s.IsActive, s.CreatedAtUtc))
                .ToListAsync(cancellationToken);
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("api/v1/system-apps", async (Handler handler, CancellationToken cancellationToken) =>
                    Results.Ok(await handler.HandleAsync(cancellationToken)))
                .RequireAuthorization(Roles.Admin)
                .WithTags("SystemApps");
        }
    }
}
