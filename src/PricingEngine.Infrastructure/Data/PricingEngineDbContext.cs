using Microsoft.EntityFrameworkCore;
using PricingEngine.Domain.Entities;
using PricingEngine.Infrastructure.Data.Configurations;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Data;

public sealed class PricingEngineDbContext : DbContext
{
    public PricingEngineDbContext(DbContextOptions<PricingEngineDbContext> options)
        : base(options) { }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<ProductConfig> ProductConfigs => Set<ProductConfig>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<QuoteAuditLog> QuoteAuditLogs => Set<QuoteAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discover all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingEngineDbContext).Assembly);
    }
}
