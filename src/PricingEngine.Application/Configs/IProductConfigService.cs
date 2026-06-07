namespace PricingEngine.Application.Configs;

public interface IProductConfigService
{
    /// <summary>Returns all active config entries for a product code.</summary>
    Task<IReadOnlyList<ProductConfigDto>> GetActiveConfigsAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a config key for a product.
    /// If the key already exists its value is updated in place.
    /// If it does not exist a new row is created (effective immediately).
    /// Returns the resulting config entry.
    /// </summary>
    Task<ProductConfigDto> UpsertAsync(
        string productCode,
        string configKey,
        UpsertConfigRequest request,
        CancellationToken cancellationToken = default);
}
