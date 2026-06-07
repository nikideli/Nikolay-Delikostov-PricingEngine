using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PricingEngine.Application.Events;
using PricingEngine.Infrastructure.Data;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Messaging;

/// <summary>
/// Background service that implements the "polling publisher" half of the Transactional Outbox pattern.
///
/// Every 5 seconds it:
///   1. Claims a batch of unprocessed outbox messages using SELECT FOR UPDATE SKIP LOCKED
///      (row-level locking prevents duplicate processing when multiple app instances run).
///   2. Publishes each message to RabbitMQ via MassTransit.
///   3. Marks each message as processed.
///
/// On publish failure the retry counter is incremented. After MaxRetries the message
/// is abandoned (marked as failed) and an alert is logged for operator intervention.
/// </summary>
public sealed class OutboxProcessorService : BackgroundService
{
    private const int BatchSize = 10;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;

    // Polly retry pipeline: exponential back-off for transient RabbitMQ failures
    private readonly ResiliencePipeline _publishRetry;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _publishRetry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("Publish retry {Attempt} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error in outbox processor batch.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PricingEngineDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var lockCutoff = DateTime.UtcNow - LockTimeout;

        // Claim messages with row-level locking to prevent duplicate processing.
        // SKIP LOCKED ensures competing instances take different rows rather than blocking.
        // Column names must be quoted to match EF Core's PascalCase PostgreSQL mapping
        var messages = await db.OutboxMessages
            .FromSqlRaw(@"
                SELECT * FROM outbox_messages
                WHERE ""ProcessedAt"" IS NULL
                  AND ""RetryCount"" < {0}
                  AND (""LockedAt"" IS NULL OR ""LockedAt"" < {1})
                ORDER BY ""CreatedAt""
                LIMIT {2}
                FOR UPDATE SKIP LOCKED",
                MaxRetries, lockCutoff, BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        // Claim the batch by setting LockedAt
        var now = DateTime.UtcNow;
        foreach (var msg in messages) msg.LockedAt = now;
        await db.SaveChangesAsync(ct);

        // Publish each message; update its status
        foreach (var msg in messages)
        {
            await PublishAndUpdateAsync(msg, bus, db, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task PublishAndUpdateAsync(
        OutboxMessage msg, IBus bus, PricingEngineDbContext db, CancellationToken ct)
    {
        try
        {
            await _publishRetry.ExecuteAsync(async token =>
                await DispatchAsync(msg, bus, token), ct);

            msg.ProcessedAt = DateTime.UtcNow;
            msg.LockedAt = null;
            msg.Error = null;

            _logger.LogInformation("Outbox message {Id} ({Type}) published.", msg.Id, msg.Type);
        }
        catch (Exception ex)
        {
            msg.RetryCount++;
            msg.LockedAt = null;
            msg.Error = ex.Message;

            if (msg.RetryCount >= MaxRetries)
                _logger.LogCritical(ex,
                    "Outbox message {Id} ({Type}) reached max retries and will not be retried.", msg.Id, msg.Type);
            else
                _logger.LogWarning(ex,
                    "Outbox message {Id} ({Type}) failed (attempt {Attempt}).", msg.Id, msg.Type, msg.RetryCount);
        }
    }

    private static Task DispatchAsync(OutboxMessage msg, IBus bus, CancellationToken ct) =>
        msg.Type switch
        {
            nameof(QuoteCreatedEvent) =>
                bus.Publish(JsonSerializer.Deserialize<QuoteCreatedEvent>(msg.Payload)!, ct),
            _ => throw new InvalidOperationException($"Unknown outbox message type: '{msg.Type}'"),
        };
}
