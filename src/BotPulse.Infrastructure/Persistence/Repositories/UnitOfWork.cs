using BotPulse.Core.Abstractions.Persistence;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IUnitOfWork.</summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly BotPulseDbContext _context;

    public UnitOfWork(BotPulseDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default) =>
        _context.Database.BeginTransactionAsync(ct)
            .ContinueWith(
                t => (IAsyncDisposable)t.Result,
                ct,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

    public async ValueTask DisposeAsync() => await _context.DisposeAsync().ConfigureAwait(false);
}
