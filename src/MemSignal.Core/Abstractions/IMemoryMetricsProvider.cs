using MemSignal.Core;

namespace MemSignal.Abstractions;

public interface IMemoryMetricsProvider
{
    ValueTask<MemoryMetricSnapshotResult> SampleAsync(CancellationToken cancellationToken);
}
