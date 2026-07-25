using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IUserRepository.</summary>
internal sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(BotPulseDbContext context) : base(context) { }

    public async Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default) =>
        await Context.Users
            .FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive, ct)
            .ConfigureAwait(false);

    public async Task<User?> FindByExternalIdAsync(string authProvider, string externalId, CancellationToken ct = default) =>
        await Context.Users
            .FirstOrDefaultAsync(u => u.AuthProvider == authProvider && u.ExternalId == externalId, ct)
            .ConfigureAwait(false);
}

/// <summary>EF Core implementation of IDashboardLayoutRepository.</summary>
internal sealed class DashboardLayoutRepository : GenericRepository<DashboardLayout>, IDashboardLayoutRepository
{
    public DashboardLayoutRepository(BotPulseDbContext context) : base(context) { }

    public async Task<DashboardLayout?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await Context.DashboardLayouts
            .FirstOrDefaultAsync(d => d.UserId == userId, ct)
            .ConfigureAwait(false);
}
