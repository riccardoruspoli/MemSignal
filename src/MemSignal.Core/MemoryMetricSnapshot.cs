namespace MemSignal.Core;

public sealed record MemoryMetricSnapshot(
    DateTimeOffset Timestamp,
    ulong CommittedBytes,
    ulong CommitLimitBytes,
    ulong AvailablePhysicalBytes,
    ulong TotalPhysicalBytes,
    double PagesPerSecond,
    double PageReadsPerSecond,
    double? PagefileUsagePercent,
    ulong? ReclaimableCacheBytes = null);
