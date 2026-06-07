using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingEngine.Domain.Entities;

namespace PricingEngine.Infrastructure.Data.Configurations;

internal sealed class ProductConfigConfiguration : IEntityTypeConfiguration<ProductConfig>
{
    // Stable GUIDs for seed data — must never change once in production
    private static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ProductConfig> builder)
    {
        builder.ToTable("product_configs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.ConfigKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ConfigValue).HasMaxLength(500).IsRequired();
        builder.Property(c => c.EffectiveFrom).HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.EffectiveTo).HasColumnType("timestamptz");

        // Unique constraint: only one active value per (product, key) combination
        builder.HasIndex(c => new { c.ProductCode, c.ConfigKey })
            .IsUnique()
            .HasDatabaseName("ix_product_configs_product_key");

        // --- Seed data for HOME_BASIC ---
        // Tariff rates and fees are DB-driven; to change them just update these rows.
        SeedHomeBasicConfigs(builder);
    }

    private static void SeedHomeBasicConfigs(EntityTypeBuilder<ProductConfig> builder)
    {
        var configs = new[]
        {
            ("tariff_rate",       "0.0018"),  // 0.18% of insured sum
            ("fixed_fee",         "25.00"),   // flat administration fee
            ("tax_rate",          "0.12"),    // 12% tax on net premium
            ("installment_fee_2x","0.015"),   // 1.5% financing surcharge for 2 installments
            ("installment_fee_4x","0.030"),   // 3.0% financing surcharge for 4 installments
        };

        // Use deterministic GUIDs so EF doesn't regenerate seed rows on every migration
        var baseGuid = new Guid("a1000000-0000-0000-0000-000000000000");
        for (var i = 0; i < configs.Length; i++)
        {
            var (key, value) = configs[i];
            builder.HasData(new
            {
                Id = new Guid($"a1{i + 1:D6}-0000-0000-0000-000000000000"),
                ProductCode = "HOME_BASIC",
                ConfigKey = key,
                ConfigValue = value,
                EffectiveFrom = SeedDate,
                EffectiveTo = (DateTime?)null,
            });
        }
    }
}
