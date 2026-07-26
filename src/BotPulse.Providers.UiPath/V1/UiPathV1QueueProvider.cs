using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1QueueProvider : IQueueProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1QueueProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathQueueDto>("odata/QueueDefinitions", ct: ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<QueueItemSnapshot>> GetQueueItemsAsync(
        QueueItemQuery query, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(query.QueueName))
        {
            filters.Add($"QueueDefinitionName eq '{query.QueueName}'");
        }

        if (query.UpdatedSinceUtc.HasValue)
        {
            filters.Add($"LastModificationTime gt {query.UpdatedSinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        var qs = $"$top={query.Top}";
        if (filters.Count > 0)
        {
            qs += $"&$filter={string.Join(" and ", filters)}";
        }

        var dtos = await _http.GetOdataAsync<UiPathQueueItemDto>("odata/QueueItems", qs, ct).ConfigureAwait(false);
        return dtos.Select(MapItem).ToList();
    }

    private static QueueSnapshot Map(UiPathQueueDto dto) => new(
        ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
        Name: dto.Name,
        TotalItems: dto.Total,
        ProcessedItems: dto.Successful,
        FailedItems: dto.Failed,
        PendingItems: dto.InProgress + dto.New);

    private static QueueItemSnapshot MapItem(UiPathQueueItemDto dto) => new(
        ExternalItemId: dto.Id.ToString(CultureInfo.InvariantCulture),
        QueueName: dto.QueueDefinitionName,
        Status: dto.Status,
        RetryCount: dto.RetryNumber,
        ProcessingStartUtc: dto.StartProcessing,
        ProcessingEndUtc: dto.EndProcessing,
        OriginalExternalItemId: null,
        OutputMetadataJson: dto.Output);

    private sealed class UiPathQueueDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Total { get; init; }
        public int Successful { get; init; }
        public int Failed { get; init; }
        public int InProgress { get; init; }
        public int New { get; init; }
    }

    private sealed class UiPathQueueItemDto
    {
        public long Id { get; init; }
        public string QueueDefinitionName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int RetryNumber { get; init; }
        public DateTime? StartProcessing { get; init; }
        public DateTime? EndProcessing { get; init; }
        public string? Output { get; init; }
    }
}
