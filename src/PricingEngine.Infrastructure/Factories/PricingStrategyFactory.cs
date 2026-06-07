using PricingEngine.Domain.Exceptions;
using PricingEngine.Domain.Strategies;

namespace PricingEngine.Infrastructure.Factories;

/// <summary>
/// Resolves the correct IPricingStrategy from the DI container's registered set.
/// All strategies are registered as IEnumerable&lt;IPricingStrategy&gt; in DI;
/// the factory builds a dictionary for O(1) lookup by product code.
///
/// Adding a new product requires one additional AddSingleton call in
/// ServiceCollectionExtensions — no changes to this class or the engine.
/// </summary>
public sealed class PricingStrategyFactory : IPricingStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IPricingStrategy> _strategies;

    public PricingStrategyFactory(IEnumerable<IPricingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            s => s.ProductCode,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    public IPricingStrategy GetStrategy(string productCode)
    {
        if (_strategies.TryGetValue(productCode, out var strategy))
            return strategy;

        throw new ProductNotFoundException(productCode);
    }

    public IReadOnlyList<string> GetAllProductCodes() =>
        _strategies.Keys.OrderBy(k => k).ToList();
}
