namespace PricingEngine.Domain.Strategies;

/// <summary>
/// Resolves the correct IPricingStrategy for a given product code.
/// Defined in Domain so the Application layer can depend on this abstraction
/// without knowing about DI infrastructure or concrete strategy types.
/// </summary>
public interface IPricingStrategyFactory
{
    /// <summary>
    /// Returns the registered strategy for the given product code.
    /// </summary>
    /// <exception cref="Exceptions.ProductNotFoundException">
    /// Thrown when no strategy is registered for <paramref name="productCode"/>.
    /// </exception>
    IPricingStrategy GetStrategy(string productCode);

    /// <summary>Returns every registered product code in alphabetical order.</summary>
    IReadOnlyList<string> GetAllProductCodes();
}
