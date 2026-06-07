using System.Text.Json;
using PricingEngine.Application.Interfaces;
using PricingEngine.Infrastructure.Data;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Repositories;

internal sealed class OutboxRepository : IOutboxRepository
{
    private readonly PricingEngineDbContext _context;

    public OutboxRepository(PricingEngineDbContext context) => _context = context;

    public void Add(string messageType, object payload)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = messageType,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
        };

        _context.OutboxMessages.Add(message);
    }
}
