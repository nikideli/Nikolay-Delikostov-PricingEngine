namespace PricingEngine.Application.Events;

/// <summary>
/// Published to RabbitMQ by the OutboxProcessorService after a quote is persisted.
/// The QuoteAuditConsumer subscribes to this event to write the audit log.
/// 
/// This event must remain backwards-compatible: only add optional fields, never remove.
/// </summary>
public sealed record QuoteCreatedEvent(
    Guid QuoteId,
    string ProductCode,
    decimal NetPremium,
    decimal Taxes,
    decimal Fees,
    decimal Total,
    string InputDataJson,
    DateTime CreatedAt);
