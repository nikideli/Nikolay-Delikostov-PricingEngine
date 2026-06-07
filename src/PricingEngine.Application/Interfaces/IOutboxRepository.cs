namespace PricingEngine.Application.Interfaces;

/// <summary>
/// Abstraction over the Outbox table.
/// The Application layer only knows it can enqueue a typed message payload;
/// the Infrastructure layer owns the OutboxMessage entity and schema.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Stages an outbox message in the EF change tracker (does not call SaveChanges).
    /// The message will be published to the message bus by OutboxProcessorService.
    /// </summary>
    void Add(string messageType, object payload);
}
