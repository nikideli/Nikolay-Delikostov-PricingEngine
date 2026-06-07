namespace PricingEngine.Infrastructure.Entities;

/// <summary>
/// The Outbox table record. An OutboxMessage is written in the same DB transaction
/// as the business entity it represents. The OutboxProcessorService later picks it up
/// and publishes it to RabbitMQ, guaranteeing at-least-once delivery without a
/// distributed transaction.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>The CLR type name of the event, used for deserialization dispatch.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Set when a processor instance claims this message.
    /// Prevents duplicate processing when multiple app instances run concurrently.
    /// A message whose LockedAt is older than 30 seconds is considered abandoned and re-claimable.
    /// </summary>
    public DateTime? LockedAt { get; set; }
}
