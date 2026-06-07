using PricingEngine.Domain.ValueObjects;

namespace PricingEngine.Domain.Strategies;

/// <summary>
/// Pricing strategy for the HOME_BASIC product.
///
/// Formula:
///   NetPremium  = Round(insuredSum × tariff_rate, 2)
///   Taxes       = Round(NetPremium × tax_rate,    2)
///   Fees        = fixed_fee  (flat, no rounding needed)
///   Total       = NetPremium + Taxes + Fees
///
/// Installment surcharges are applied to Total before splitting:
///   1-installment: no surcharge
///   2-installment: Total × (1 + installment_fee_2x)
///   4-installment: Total × (1 + installment_fee_4x)
///
/// All config values are loaded from the product_configs table, so tariff
/// changes are live after a DB update — no redeployment required.
/// </summary>
public sealed class HomeBasicPricingStrategy : IPricingStrategy
{
    public string ProductCode => "HOME_BASIC";

    public Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken cancellationToken = default)
    {
        // --- Read inputs (product-specific fields) ---
        var insuredSum = context.GetRequiredDecimal("insuredSum");

        if (insuredSum <= 0)
            throw new Exceptions.InvalidInputException("insuredSum", "must be greater than zero");

        // --- Read configuration from DB (tariff, fees, tax) ---
        var tariffRate = context.GetConfigDecimal("tariff_rate");
        var fixedFee = context.GetConfigDecimal("fixed_fee");
        var taxRate = context.GetConfigDecimal("tax_rate");
        var instFee2x = context.GetConfigDecimal("installment_fee_2x");
        var instFee4x = context.GetConfigDecimal("installment_fee_4x");

        // --- Calculate breakdown ---
        var netPremium = Round(insuredSum * tariffRate);
        var taxes = Round(netPremium * taxRate);
        var fees = fixedFee;

        var breakdown = new PriceBreakdown(netPremium, taxes, fees);

        // --- Calculate installment plans ---
        var total = breakdown.Total;
        var plans = new[]
        {
            BuildPlan(1, total, 0m),
            BuildPlan(2, total, instFee2x),
            BuildPlan(4, total, instFee4x),
        };

        return Task.FromResult(new PricingResult(breakdown, plans));
    }

    // Builds an installment plan with an optional financing surcharge.
    private static InstallmentPlan BuildPlan(int count, decimal baseTotal, decimal surchargeRate)
    {
        var planTotal = Round(baseTotal * (1m + surchargeRate));
        var amountPerInstalment = Round(planTotal / count);
        return new InstallmentPlan(count, amountPerInstalment, planTotal);
    }

    // Banker's rounding is NOT appropriate for insurance; AwayFromZero is the industry standard.
    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
