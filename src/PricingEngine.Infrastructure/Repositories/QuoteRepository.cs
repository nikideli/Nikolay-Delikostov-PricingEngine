using Microsoft.EntityFrameworkCore;
using PricingEngine.Application.Interfaces;
using PricingEngine.Domain.Entities;
using PricingEngine.Infrastructure.Data;

namespace PricingEngine.Infrastructure.Repositories;

internal sealed class QuoteRepository : IQuoteRepository
{
    private readonly PricingEngineDbContext _context;

    public QuoteRepository(PricingEngineDbContext context) => _context = context;

    public void Add(Quote quote) => _context.Quotes.Add(quote);

    public Task<Quote?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Quotes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
}
