using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PricingEngine.Application.Configs;
using PricingEngine.Application.Interfaces;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain.Strategies;
using PricingEngine.Infrastructure.Data;
using PricingEngine.Infrastructure.Factories;
using PricingEngine.Infrastructure.Messaging;
using PricingEngine.Infrastructure.Messaging.Consumers;
using PricingEngine.Infrastructure.Repositories;

namespace PricingEngine.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Database ---
        services.AddDbContext<PricingEngineDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5)));

        // --- Repositories & Unit of Work ---
        // Scoped: one instance per HTTP request, shared within that request
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IProductConfigRepository, ProductConfigRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- Application services ---
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IProductConfigService, ProductConfigService>();

        // --- Pricing Strategies ---
        // Each strategy is stateless; Singleton is correct and avoids repeated allocations.
        // *** TO ADD A NEW PRODUCT: add one line here ***
        services.AddSingleton<IPricingStrategy, HomeBasicPricingStrategy>();
        // services.AddSingleton<IPricingStrategy, MotorComprehensivePricingStrategy>();  ← example

        // --- Strategy Factory ---
        // Singleton: built once from the registered IEnumerable<IPricingStrategy>
        services.AddSingleton<IPricingStrategyFactory>(sp =>
            new PricingStrategyFactory(sp.GetServices<IPricingStrategy>()));

        // --- MassTransit + RabbitMQ ---
        services.AddMassTransit(x =>
        {
            // Register consumers — MassTransit creates the queue automatically
            x.AddConsumer<QuoteAuditConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ"));

                // Retry policy: 3 immediate retries, then dead-letter
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        // --- Outbox processor background service ---
        services.AddHostedService<OutboxProcessorService>();

        return services;
    }
}
