namespace PricingEngine.Application.Quotes;

public sealed record CreateQuoteResponse(
    Guid QuoteId,
    string ProductCode,
    PriceBreakdownDto Breakdown,
    IReadOnlyList<InstallmentPlanDto> InstallmentPlans,
    DateTime CreatedAt);

public sealed record PriceBreakdownDto(
    decimal NetPremium,
    decimal Taxes,
    decimal Fees,
    decimal Total);

public sealed record InstallmentPlanDto(
    int NumberOfInstallments,
    decimal AmountPerInstallment,
    decimal TotalAmount);
