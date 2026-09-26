namespace ConejitoTicket.Api.Domain;

public sealed class SystemApp
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
