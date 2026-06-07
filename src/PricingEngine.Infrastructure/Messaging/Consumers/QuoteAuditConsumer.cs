using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PricingEngine.Application.Events;
using PricingEngine.Infrastructure.Data;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes QuoteCreatedEvent messages from RabbitMQ and writes a permanent audit record.
///
/// This consumer runs on the asynchronous path, completely decoupled from the HTTP request.
/// The Transactional Outbox guarantees this consumer eventually receives every event,
/// even if RabbitMQ was temporarily unavailable at quote-creation time.
///
/// MassTransit provides automatic retries and dead-lettering if this consumer throws,
/// further reinforcing the resilience story.
/// </summary>
public sealed class QuoteAuditConsumer : IConsumer<QuoteCreatedEvent>
{
    private readonly PricingEngineDbContext _db;
    private readonly ILogger<QuoteAuditConsumer> _logger;

    public QuoteAuditConsumer(PricingEngineDbContext db, ILogger<QuoteAuditConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<QuoteCreatedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "Audit: received QuoteCreatedEvent for quote {QuoteId}, product {ProductCode}, total {Total}",
            evt.QuoteId, evt.ProductCode, evt.Total);

        var auditEntry = new QuoteAuditLog
        {
            Id = Guid.NewGuid(),
            QuoteId = evt.QuoteId,
            ProductCode = evt.ProductCode,
            EventType = nameof(QuoteCreatedEvent),
            Payload = JsonSerializer.Serialize(evt),
            ReceivedAt = DateTime.UtcNow,
        };

        _db.QuoteAuditLogs.Add(auditEntry);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Audit record written for quote {QuoteId}.", evt.QuoteId);
    }
}
