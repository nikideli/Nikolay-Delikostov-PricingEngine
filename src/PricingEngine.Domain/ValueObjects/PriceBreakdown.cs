namespace PricingEngine.Domain.ValueObjects;

/// <summary>
/// Represents the monetary breakdown of a calculated premium.
/// All arithmetic uses decimal to guarantee precision — no float/double.
/// </summary>
public sealed record PriceBreakdown
{
    public decimal NetPremium { get; }
    public decimal Taxes { get; }
    public decimal Fees { get; }
    public decimal Total => NetPremium + Taxes + Fees;

    public PriceBreakdown(decimal netPremium, decimal taxes, decimal fees)
    {
        if (netPremium < 0) throw new ArgumentOutOfRangeException(nameof(netPremium), "Net premium cannot be negative.");
        if (taxes < 0) throw new ArgumentOutOfRangeException(nameof(taxes), "Taxes cannot be negative.");
        if (fees < 0) throw new ArgumentOutOfRangeException(nameof(fees), "Fees cannot be negative.");

        NetPremium = netPremium;
        Taxes = taxes;
        Fees = fees;
    }
}
