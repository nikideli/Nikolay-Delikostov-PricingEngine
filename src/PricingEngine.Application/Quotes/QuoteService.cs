using System.Text.Json;
using Microsoft.Extensions.Logging;
using PricingEngine.Application.Events;
using PricingEngine.Application.Interfaces;
using PricingEngine.Domain.Entities;
using PricingEngine.Domain.Strategies;
using PricingEngine.Domain.ValueObjects;

namespace PricingEngine.Application.Quotes;

/// <summary>
/// Orchestrates the quote creation flow:
///
///   1. Resolve the correct IPricingStrategy for the product code.
///   2. Load product configuration from the database.
///   3. Run the pure calculation (no DB, no I/O inside the strategy).
///   4. Persist the Quote and the OutboxMessage in a single DB transaction.
///   5. Return the response. The OutboxProcessorService will publish the event asynchronously.
///
/// The service owns the transaction boundary so that the audit trail is guaranteed
/// even if the message bus is temporarily unavailable.
/// </summary>
public sealed class QuoteService : IQuoteService
{
    private readonly IPricingStrategyFactory _strategyFactory;
    private readonly IProductConfigRepository _configRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IPricingStrategyFactory strategyFactory,
        IProductConfigRepository configRepository,
        IQuoteRepository quoteRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<QuoteService> logger)
    {
        _strategyFactory = strategyFactory;
        _configRepository = configRepository;
        _quoteRepository = quoteRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateQuoteResponse> CreateQuoteAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating quote for product {ProductCode}", request.ProductCode);

        // 1. Resolve strategy — throws ProductNotFoundException for unknown products
        var strategy = _strategyFactory.GetStrategy(request.ProductCode);

        // 2. Load live product configuration (tariff rates, fees, etc.)
        var configs = await _configRepository.GetByProductCodeAsync(request.ProductCode, cancellationToken);
        var configDict = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue, StringComparer.OrdinalIgnoreCase);

        // 3. Run the pure pricing calculation
        var context = new PricingContext(request.Inputs, configDict);
        var result = await strategy.CalculateAsync(context, cancellationToken);

        // 4. Build the Quote aggregate
        var inputJson = request.Inputs.ToJsonString();
        var plansJson = JsonSerializer.Serialize(result.InstallmentPlans);

        var quote = Quote.Create(
            request.ProductCode,
            inputJson,
            result.Breakdown.NetPremium,
            result.Breakdown.Taxes,
            result.Breakdown.Fees,
            plansJson);

        // 5. Persist quote + outbox message atomically inside a retriable transaction.
        //    NpgsqlRetryingExecutionStrategy requires the entire begin/save/commit
        //    to run inside CreateExecutionStrategy().ExecuteAsync() — hence the callback.
        await _unitOfWork.ExecuteInTransactionAsync(() =>
        {
            _quoteRepository.Add(quote);
            _outboxRepository.Add(
                nameof(QuoteCreatedEvent),
                new QuoteCreatedEvent(
                    quote.Id,
                    quote.ProductCode,
                    result.Breakdown.NetPremium,
                    result.Breakdown.Taxes,
                    result.Breakdown.Fees,
                    result.Breakdown.Total,
                    inputJson,
                    quote.CreatedAt));
            return Task.CompletedTask;
        }, cancellationToken);

        _logger.LogInformation("Quote {QuoteId} created for product {ProductCode}, total = {Total}",
            quote.Id, quote.ProductCode, result.Breakdown.Total);

        return MapToResponse(quote, result);
    }

    public async Task<CreateQuoteResponse?> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quote = await _quoteRepository.FindByIdAsync(id, cancellationToken);
        if (quote is null) return null;

        // Reconstruct the response from the stored entity
        var plans = JsonSerializer.Deserialize<List<InstallmentPlan>>(quote.InstallmentPlansJson)
                    ?? new List<InstallmentPlan>();

        var breakdown = new PriceBreakdown(quote.NetPremium, quote.Taxes, quote.Fees);
        var result = new PricingResult(breakdown, plans);
        return MapToResponse(quote, result);
    }

    private static CreateQuoteResponse MapToResponse(Quote quote, PricingResult result) =>
        new(
            QuoteId: quote.Id,
            ProductCode: quote.ProductCode,
            Breakdown: new PriceBreakdownDto(
                result.Breakdown.NetPremium,
                result.Breakdown.Taxes,
                result.Breakdown.Fees,
                result.Breakdown.Total),
            InstallmentPlans: result.InstallmentPlans
                .Select(p => new InstallmentPlanDto(p.NumberOfInstallments, p.AmountPerInstallment, p.TotalAmount))
                .ToList(),
            CreatedAt: quote.CreatedAt);
}
