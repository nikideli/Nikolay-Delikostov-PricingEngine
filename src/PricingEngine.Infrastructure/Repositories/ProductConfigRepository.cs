using Microsoft.EntityFrameworkCore;
using PricingEngine.Application.Interfaces;
using PricingEngine.Domain.Entities;
using PricingEngine.Infrastructure.Data;

namespace PricingEngine.Infrastructure.Repositories;

internal sealed class ProductConfigRepository : IProductConfigRepository
{
    private readonly PricingEngineDbContext _context;

    public ProductConfigRepository(PricingEngineDbContext context) => _context = context;

    public Task<IReadOnlyList<ProductConfig>> GetByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default) =>
        _context.ProductConfigs
            .AsNoTracking()
            .Where(c => c.ProductCode == productCode
                     && c.EffectiveFrom <= DateTime.UtcNow
                     && (c.EffectiveTo == null || c.EffectiveTo > DateTime.UtcNow))
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ProductConfig>)t.Result, cancellationToken);

    public Task<ProductConfig?> FindAsync(
        string productCode,
        string configKey,
        CancellationToken cancellationToken = default) =>
        _context.ProductConfigs
            .FirstOrDefaultAsync(
                c => c.ProductCode == productCode && c.ConfigKey == configKey,
                cancellationToken);

    public void Add(ProductConfig config) => _context.ProductConfigs.Add(config);
}
