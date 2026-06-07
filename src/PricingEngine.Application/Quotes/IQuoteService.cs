namespace PricingEngine.Application.Quotes;

public interface IQuoteService
{
    Task<CreateQuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);
    Task<CreateQuoteResponse?> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default);
}
