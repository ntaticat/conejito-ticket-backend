using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using ConejitoTicket.Api.Domain;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace ConejitoTicket.Api.Infrastructure;

public sealed class VapidOptions
{
    public string? Subject { get; init; }
    public string? PublicKey { get; init; }
    public string? PrivateKey { get; init; }
}

public record TicketCreated(Guid Id, long TicketNumber, string Title, TicketPriority Priority);

// Crear un ticket solo encola; el envío ocurre en segundo plano y nunca hace fallar la petición.
// ponytail: cola en memoria, los envíos pendientes se pierden si el API se reinicia. Tabla outbox si eso importa.
public sealed class PushNotifier(
    IServiceScopeFactory scopes, IOptions<VapidOptions> options, ILogger<PushNotifier> logger) : BackgroundService
{
    private readonly Channel<TicketCreated> queue =
        Channel.CreateUnbounded<TicketCreated>(new UnboundedChannelOptions { SingleReader = true });

    private readonly bool enabled = options.Value is { Subject.Length: > 0, PublicKey.Length: > 0, PrivateKey.Length: > 0 };

    public void Enqueue(TicketCreated ticket)
    {
        if (enabled) queue.Writer.TryWrite(ticket);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!enabled)
        {
            logger.LogWarning("Faltan Vapid:Subject/PublicKey/PrivateKey: las notificaciones push están desactivadas.");
            return;
        }

        var vapid = options.Value;
        using var http = new HttpClient();
        var client = new PushServiceClient(http)
        {
            DefaultAuthentication = new VapidAuthentication(vapid.PublicKey, vapid.PrivateKey) { Subject = vapid.Subject },
        };

        await foreach (var ticket in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(client, ticket, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Error enviando push del ticket {TicketId}", ticket.Id);
            }
        }
    }

    private async Task SendAsync(PushServiceClient client, TicketCreated ticket, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Formato que espera el service worker de la PWA (src/sw.ts).
        var payload = JsonSerializer.Serialize(new
        {
            title = $"Nuevo ticket #{ticket.TicketNumber} · {PriorityLabel(ticket.Priority)}",
            body = ticket.Title,
            url = $"/tickets/{ticket.Id}",
            tag = ticket.Id.ToString(),
        });

        foreach (var subscription in await context.PushSubscriptions.ToListAsync(cancellationToken))
        {
            var target = new WebPushSubscription { Endpoint = subscription.Endpoint };
            target.SetKey(PushEncryptionKeyName.P256DH, subscription.P256dh);
            target.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);

            try
            {
                await client.RequestPushMessageDeliveryAsync(target, new PushMessage(payload), cancellationToken);
            }
            catch (PushServiceClientException e) when (e.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // El navegador anuló la suscripción: ya no sirve.
                context.PushSubscriptions.Remove(subscription);
            }
            catch (PushServiceClientException e)
            {
                logger.LogWarning(e, "Push rechazado ({Status}) para la suscripción {SubscriptionId}", e.StatusCode, subscription.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string PriorityLabel(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Baja",
        TicketPriority.Medium => "Media",
        TicketPriority.High => "Alta",
        TicketPriority.Critical => "Crítica",
        _ => priority.ToString(),
    };
}
