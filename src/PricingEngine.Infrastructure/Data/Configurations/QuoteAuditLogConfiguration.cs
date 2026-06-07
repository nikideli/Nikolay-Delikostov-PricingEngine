using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingEngine.Infrastructure.Entities;

namespace PricingEngine.Infrastructure.Data.Configurations;

internal sealed class QuoteAuditLogConfiguration : IEntityTypeConfiguration<QuoteAuditLog>
{
    public void Configure(EntityTypeBuilder<QuoteAuditLog> builder)
    {
        builder.ToTable("quote_audit_log");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuoteId).IsRequired();
        builder.Property(a => a.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EventType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.ReceivedAt).HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(a => a.QuoteId).HasDatabaseName("ix_audit_quote_id");
    }
}
