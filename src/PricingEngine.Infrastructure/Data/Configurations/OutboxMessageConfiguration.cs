using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Data.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(m => m.ProcessedAt).HasColumnType("timestamptz");
        builder.Property(m => m.LockedAt).HasColumnType("timestamptz");
        builder.Property(m => m.Error).HasMaxLength(2000);

        // The processor queries unprocessed messages; this index keeps that query fast
        builder.HasIndex(m => new { m.ProcessedAt, m.RetryCount })
            .HasDatabaseName("ix_outbox_unprocessed");
    }
}
