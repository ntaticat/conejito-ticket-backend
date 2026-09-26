namespace ConejitoTicket.Api.Domain;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AdminUserId { get; init; }
    // Todos los tokens rotados desde un mismo login comparten familia; reusar uno revocado revoca la familia.
    public Guid FamilyId { get; init; }
    public required string TokenHash { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; set; }
}
