using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Specialized repository for User entities.</summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
    Task<User?> FindByExternalIdAsync(string authProvider, string externalId, CancellationToken ct = default);
}

/// <summary>Repository for per-user dashboard layout configurations.</summary>
public interface IDashboardLayoutRepository : IRepository<DashboardLayout>
{
    Task<DashboardLayout?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
