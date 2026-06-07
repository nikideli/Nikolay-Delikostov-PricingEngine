using Microsoft.EntityFrameworkCore;
using PricingEngine.Application.Interfaces;
using PricingEngine.Infrastructure.Data;

namespace PricingEngine.Infrastructure.Repositories;

/// <summary>
/// Coordinates EF Core transactions across repositories that share the same DbContext.
///
/// Uses CreateExecutionStrategy().ExecuteAsync() so that the retry-on-failure
/// strategy (NpgsqlRetryingExecutionStrategy) can wrap the entire Begin/Save/Commit
/// sequence, retrying it atomically on transient database errors.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly PricingEngineDbContext _context;

    public UnitOfWork(PricingEngineDbContext context) => _context = context;

    public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
