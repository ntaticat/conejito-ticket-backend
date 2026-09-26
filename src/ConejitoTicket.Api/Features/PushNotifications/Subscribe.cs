using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ConejitoTicket.Api.Domain;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Features.PushNotifications;

public static class Subscribe
{
    // Mismo shape que PushSubscription.toJSON() del navegador.
    public record SubscribeRequest([Required, Url, MaxLength(1000)] string Endpoint, [Required] SubscriptionKeys Keys);

    public record SubscriptionKeys([Required, MaxLength(200)] string P256dh, [Required, MaxLength(100)] string Auth);

    public record SubscriptionDto(Guid Id, string Endpoint);

    public sealed class Handler(AppDbContext context)
    {
        public async Task<SubscriptionDto> HandleAsync(
            SubscribeRequest request, Guid adminUserId, CancellationToken cancellationToken)
        {
            var subscription = await context.PushSubscriptions
                .SingleOrDefaultAsync(s => s.Endpoint == request.Endpoint, cancellationToken);

            if (subscription is null)
            {
                subscription = new PushSubscription
                {
                    Endpoint = request.Endpoint,
                    P256dh = request.Keys.P256dh,
                    Auth = request.Keys.Auth,
                };
                context.PushSubscriptions.Add(subscription);
            }

            subscription.AdminUserId = adminUserId;
            subscription.P256dh = request.Keys.P256dh;
            subscription.Auth = request.Keys.Auth;

            await context.SaveChangesAsync(cancellationToken);

            return new SubscriptionDto(subscription.Id, subscription.Endpoint);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/push/subscriptions", async (
                    [FromBody] SubscribeRequest request,
                    ClaimsPrincipal user,
                    Handler handler,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await handler.HandleAsync(request, user.GetSubjectId(), cancellationToken)))
                .RequireAuthorization(Roles.Admin)
                .WithTags("PushNotifications");
        }
    }
}
