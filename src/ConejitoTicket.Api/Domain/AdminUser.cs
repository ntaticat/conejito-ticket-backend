namespace ConejitoTicket.Api.Domain;

public sealed class AdminUser
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string UserName { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
