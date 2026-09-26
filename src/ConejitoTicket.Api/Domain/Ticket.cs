namespace ConejitoTicket.Api.Domain;

public enum TicketType { Bug, FeatureRequest, Question, Improvement }

public enum TicketPriority { Low, Medium, High, Critical }

public enum TicketStatus { New, InProgress, Resolved, Closed }

public sealed class Ticket
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public long TicketNumber { get; private set; }
    public Guid SystemAppId { get; init; }
    public SystemApp? SystemApp { get; private set; }
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? ReporterUserId { get; set; }
    public string? ReporterUserName { get; set; }
    public string? ReporterUserEmail { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketType Type { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public List<TicketAttachment> Attachments { get; init; } = [];
}
