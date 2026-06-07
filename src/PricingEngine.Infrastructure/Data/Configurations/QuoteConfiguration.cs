using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingEngine.Domain.Entities;

namespace PricingEngine.Infrastructure.Data.Configurations;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.ProductCode)
            .HasMaxLength(50)
            .IsRequired();

        // JSONB column: any product's input shape fits without a migration
        builder.Property(q => q.InputDataJson)
            .HasColumnName("input_data")
            .HasColumnType("jsonb")
            .IsRequired();

        // NUMERIC(18,4) is precise enough for monetary values up to ~1 trillion
        builder.Property(q => q.NetPremium).HasColumnType("numeric(18,4)");
        builder.Property(q => q.Taxes).HasColumnType("numeric(18,4)");
        builder.Property(q => q.Fees).HasColumnType("numeric(18,4)");

        // JSONB column: installment plans stored flexibly
        builder.Property(q => q.InstallmentPlansJson)
            .HasColumnName("installment_plans")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // Allows filtering/reporting by product without a full scan
        builder.HasIndex(q => q.ProductCode).HasDatabaseName("ix_quotes_product_code");
        builder.HasIndex(q => q.CreatedAt).HasDatabaseName("ix_quotes_created_at");
    }
}
