using PricingEngine.Domain.Entities;

namespace PricingEngine.Application.Interfaces;

public interface IProductConfigRepository
{
    /// <summary>Returns all active configuration entries for a product as of now.</summary>
    Task<IReadOnlyList<ProductConfig>> GetByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a specific config row by product code and key (active or not).</summary>
    Task<ProductConfig?> FindAsync(
        string productCode,
        string configKey,
        CancellationToken cancellationToken = default);

    void Add(ProductConfig config);
}
