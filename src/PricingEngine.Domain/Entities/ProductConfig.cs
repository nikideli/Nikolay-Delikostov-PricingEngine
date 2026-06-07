namespace PricingEngine.Domain.Entities;

/// <summary>
/// A single key-value configuration entry for a product.
/// Together these rows define tariffs, fees, and coefficients without any code change.
///
/// To update a tariff: INSERT a new row with a future effective_from date,
/// or UPDATE the existing row. No migration or redeploy required.
/// </summary>
public sealed class ProductConfig
{
    public Guid Id { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string ConfigKey { get; private set; } = string.Empty;

    /// <summary>
    /// The config value stored as a plain string (parseable as decimal, int, bool, or JSON).
    /// Keeping it a string gives maximum flexibility for different value types.
    /// </summary>
    public string ConfigValue { get; private set; } = string.Empty;

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    // Required by EF Core
    private ProductConfig() { }

    public static ProductConfig Create(
        string productCode,
        string configKey,
        string configValue,
        DateTime? effectiveFrom = null)
    {
        return new ProductConfig
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            ConfigKey = configKey,
            ConfigValue = configValue,
            EffectiveFrom = effectiveFrom ?? DateTime.UtcNow,
        };
    }

    /// <summary>Changes the config value in place. The effective date range is unchanged.</summary>
    public void UpdateValue(string newValue) => ConfigValue = newValue;
}
