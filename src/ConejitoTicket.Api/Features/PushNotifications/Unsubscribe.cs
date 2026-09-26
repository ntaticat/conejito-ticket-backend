using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.PushNotifications;

public static class Unsubscribe
{
    public record UnsubscribeRequest([Required, MaxLength(1000)] string Endpoint);

    public sealed class Handler(AppDbContext context)
    {
        // Solo borra suscripciones del propio admin; idempotente.
        public Task<int> HandleAsync(string endpoint, Guid adminUserId, CancellationToken cancellationToken) =>
            context.PushSubscriptions
                .Where(s => s.Endpoint == endpoint && s.AdminUserId == adminUserId)
                .ExecuteDeleteAsync(cancellationToken);
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/v1/push/subscriptions", async (
                    [FromBody] UnsubscribeRequest request,
                    ClaimsPrincipal user,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                {
                    await handler.HandleAsync(request.Endpoint, user.GetSubjectId(), cancellationToken);
                    return Results.NoContent();
                })
                .RequireAuthorization(Roles.Admin)
                .WithTags("PushNotifications");
        }
    }
}
