using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>Append-only EF Core implementation of IAuditRepository.</summary>
internal sealed class AuditRepository : IAuditRepository
{
    private readonly BotPulseDbContext _context;

    public AuditRepository(BotPulseDbContext context) => _context = context;

    public async Task RecordAsync(AuditRecordData record, CancellationToken ct = default)
    {
        var entity = AuditRecord.For(record);
        await _context.AuditRecords.AddAsync(entity, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditRecordData>> QueryAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = _context.AuditRecords.AsQueryable();

        if (!string.IsNullOrEmpty(query.UserId))
        {
            q = q.Where(a => a.UserId == query.UserId);
        }

        if (!string.IsNullOrEmpty(query.Action))
        {
            q = q.Where(a => a.Action == query.Action);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(a => a.TimestampUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            q = q.Where(a => a.TimestampUtc <= query.ToUtc.Value);
        }

        var results = await q
            .OrderByDescending(a => a.TimestampUtc)
            .Take(query.Top)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return results
            .Select(a => new AuditRecordData(
                a.UserId, a.UserName, a.Action, a.ResourceType, a.ResourceId,
                a.Outcome, a.IpAddress, a.CorrelationId, a.DetailsJson, a.TimestampUtc))
            .ToList();
    }
}
