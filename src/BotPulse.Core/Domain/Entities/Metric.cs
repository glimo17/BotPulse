namespace BotPulse.Core.Domain.Entities;

/// <summary>Raw metric data point collected at a specific point in time.</summary>
public sealed class MetricPoint
{
    private MetricPoint() { }

    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string MetricName { get; private set; } = string.Empty;
    public double Value { get; private set; }
    public string DimensionsJson { get; private set; } = "{}";
    public string ProviderName { get; private set; } = string.Empty;

    public static MetricPoint Create(string metricName, double value, string providerName, string dimensionsJson = "{}")
    {
        return new MetricPoint
        {
            TimestampUtc = DateTime.UtcNow,
            MetricName = metricName,
            Value = value,
            ProviderName = providerName,
            DimensionsJson = dimensionsJson,
        };
    }
}

/// <summary>Aggregated metric rollup for a specific time bucket (hourly or daily).</summary>
public sealed class MetricRollup
{
    private MetricRollup() { }

    public long Id { get; private set; }
    public DateTime BucketStartUtc { get; private set; }
    public string Granularity { get; private set; } = string.Empty;
    public string MetricName { get; private set; } = string.Empty;
    public double SumValue { get; private set; }
    public double MinValue { get; private set; }
    public double MaxValue { get; private set; }
    public double AvgValue { get; private set; }
    public long CountValue { get; private set; }
    public string DimensionsJson { get; private set; } = "{}";

    public static MetricRollup Create(DateTime bucketStart, string granularity, string metricName,
        double sum, double min, double max, double avg, long count, string dimensionsJson = "{}")
    {
        return new MetricRollup
        {
            BucketStartUtc = bucketStart,
            Granularity = granularity,
            MetricName = metricName,
            SumValue = sum,
            MinValue = min,
            MaxValue = max,
            AvgValue = avg,
            CountValue = count,
            DimensionsJson = dimensionsJson,
        };
    }
}
