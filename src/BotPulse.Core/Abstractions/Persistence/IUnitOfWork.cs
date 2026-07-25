namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Coordinates the work of multiple repositories in a single transaction.</summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default);
}
