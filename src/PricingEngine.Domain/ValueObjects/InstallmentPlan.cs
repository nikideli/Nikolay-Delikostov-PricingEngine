namespace PricingEngine.Domain.ValueObjects;

/// <summary>
/// Represents one payment plan option (e.g., pay in 1, 2, or 4 installments).
/// amountPerInstallment * numberOfInstallments may differ from totalAmount by at most 1 cent
/// due to rounding; totalAmount is always the authoritative figure.
/// </summary>
public sealed record InstallmentPlan
{
    public int NumberOfInstallments { get; init; }
    public decimal AmountPerInstallment { get; init; }
    public decimal TotalAmount { get; init; }

    // Parameterless constructor for System.Text.Json deserialization
    public InstallmentPlan() { }

    public InstallmentPlan(int numberOfInstallments, decimal amountPerInstallment, decimal totalAmount)
    {
        NumberOfInstallments = numberOfInstallments;
        AmountPerInstallment = amountPerInstallment;
        TotalAmount = totalAmount;
    }
}
