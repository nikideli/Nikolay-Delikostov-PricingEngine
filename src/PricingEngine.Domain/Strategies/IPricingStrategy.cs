namespace PricingEngine.Domain.Strategies;

/// <summary>
/// The extension point for the Pricing Engine.
///
/// Each product (Home Basic, Motor Comprehensive, Travel, etc.) provides exactly one
/// implementation of this interface. The engine never references concrete strategy classes —
/// it depends only on this abstraction.
///
/// Strategy implementations are registered in the DI container. Adding a new product
/// means: implement this interface + one AddSingleton line. No engine code changes.
/// </summary>
public interface IPricingStrategy
{
    /// <summary>
    /// Unique, case-insensitive product code that identifies this strategy.
    /// Must match the 'productCode' field in API requests and in the product_configs table.
    /// Example: "HOME_BASIC", "MOTOR_COMPREHENSIVE"
    /// </summary>
    string ProductCode { get; }

    /// <summary>
    /// Calculates the full price for a quote request.
    /// Implementations must be stateless and deterministic.
    /// </summary>
    Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken cancellationToken = default);
}
