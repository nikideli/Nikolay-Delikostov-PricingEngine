using PricingEngine.Domain.Entities;

namespace PricingEngine.Application.Interfaces;

public interface IQuoteRepository
{
    /// <summary>Stages a Quote in the EF change tracker (does not call SaveChanges).</summary>
    void Add(Quote quote);

    Task<Quote?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
