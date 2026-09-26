namespace ConejitoTicket.Api.Domain;

public sealed class PushSubscription
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AdminUserId { get; set; }
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
