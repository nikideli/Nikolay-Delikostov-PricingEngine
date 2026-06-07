namespace PricingEngine.Domain.Entities;

/// <summary>
/// A persisted insurance product quote.
/// Created via the static factory method; the parameterless constructor is for EF Core only.
/// </summary>
public sealed class Quote
{
    public Guid Id { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;

    /// <summary>
    /// The product-specific input payload serialised as JSON.
    /// Stored as JSONB in PostgreSQL — no schema migration needed for new products.
    /// </summary>
    public string InputDataJson { get; private set; } = "{}";

    public decimal NetPremium { get; private set; }
    public decimal Taxes { get; private set; }
    public decimal Fees { get; private set; }

    /// <summary>
    /// The three installment plans serialised as a JSON array.
    /// Stored as JSONB so the structure can evolve without a schema change.
    /// </summary>
    public string InstallmentPlansJson { get; private set; } = "[]";

    public DateTime CreatedAt { get; private set; }

    // Required by EF Core
    private Quote() { }

    public static Quote Create(
        string productCode,
        string inputDataJson,
        decimal netPremium,
        decimal taxes,
        decimal fees,
        string installmentPlansJson)
    {
        return new Quote
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            InputDataJson = inputDataJson,
            NetPremium = netPremium,
            Taxes = taxes,
            Fees = fees,
            InstallmentPlansJson = installmentPlansJson,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
