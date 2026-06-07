using PricingEngine.Domain.ValueObjects;

namespace PricingEngine.Domain.Strategies;

/// <summary>
/// The calculation result returned by every IPricingStrategy.
/// </summary>
public sealed record PricingResult(
    PriceBreakdown Breakdown,
    IReadOnlyList<InstallmentPlan> InstallmentPlans);
