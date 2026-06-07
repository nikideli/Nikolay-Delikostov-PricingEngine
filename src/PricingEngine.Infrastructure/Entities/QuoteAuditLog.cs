namespace PricingEngine.Infrastructure.Entities;

/// <summary>
/// Written by the QuoteAuditConsumer when a QuoteCreatedEvent is received from RabbitMQ.
/// Provides the permanent audit trail required for regulatory compliance.
/// Intentionally append-only — no updates or deletes.
/// </summary>
public sealed class QuoteAuditLog
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;

    /// <summary>Full JSON payload of the event, stored as JSONB for queryability.</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }
}
