namespace ConejitoTicket.Api.Domain;

public sealed class TicketAttachment
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TicketId { get; init; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StoragePath { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
