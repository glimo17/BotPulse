using System.Linq.Expressions;
using BotPulse.Core.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>Generic EF Core repository implementation.</summary>
internal class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly BotPulseDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(BotPulseDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(predicate, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.Where(predicate).ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await DbSet.AddAsync(entity, ct).ConfigureAwait(false);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
