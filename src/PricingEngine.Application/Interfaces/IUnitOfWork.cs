namespace PricingEngine.Application.Interfaces;

/// <summary>
/// Coordinates a database transaction across multiple repositories.
/// Both the Quote record and the OutboxMessage must be committed atomically —
/// that is the entire point of the Transactional Outbox pattern.
///
/// The callback pattern is required because EF Core's NpgsqlRetryingExecutionStrategy
/// (enabled via EnableRetryOnFailure) cannot be used with manually opened transactions.
/// Wrapping the whole unit of work inside ExecuteAsync lets Npgsql retry the entire
/// transaction — including the Begin/Commit — on transient failures.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Executes <paramref name="operation"/> inside a retryable database transaction.
    /// SaveChanges and Commit are called automatically after the operation succeeds.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
