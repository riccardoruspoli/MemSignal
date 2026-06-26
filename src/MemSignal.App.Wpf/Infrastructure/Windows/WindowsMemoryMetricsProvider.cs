using System.ComponentModel;
using System.Runtime.InteropServices;
using MemSignal.Abstractions;
using MemSignal.Core;

namespace MemSignal.Infrastructure.Windows;

public sealed class WindowsMemoryMetricsProvider : IMemoryMetricsProvider, IDisposable
{
    private readonly PdhQuery _pdhQuery;
    private readonly PdhCounter _pagesPerSecond;
    private readonly PdhCounter _pageReadsPerSecond;
    private readonly PdhCounter? _pagefileUsage;
    private bool _disposed;

    public WindowsMemoryMetricsProvider()
    {
        var pdhQuery = PdhQuery.Open();
        try
        {
            var pagesPerSecond = pdhQuery.AddRequiredEnglishCounter(@"\Memory\Pages/sec");
            var pageReadsPerSecond = pdhQuery.AddRequiredEnglishCounter(@"\Memory\Page Reads/sec");
            var pagefileUsage = pdhQuery.TryAddEnglishCounter(@"\Paging File(_Total)\% Usage");
            pdhQuery.Collect();

            _pdhQuery = pdhQuery;
            _pagesPerSecond = pagesPerSecond;
            _pageReadsPerSecond = pageReadsPerSecond;
            _pagefileUsage = pagefileUsage;
        }
        catch
        {
            pdhQuery.Dispose();
            throw;
        }
    }

    public ValueTask<MemoryMetricSnapshotResult> SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var performanceInfo = NativeMethods.GetPerformanceInformation();

            _pdhQuery.Collect();

            var pageSize = performanceInfo.PageSize.ToUInt64();
            var snapshot = new MemoryMetricSnapshot(
                Timestamp: DateTimeOffset.Now,
                CommittedBytes: checked(performanceInfo.CommitTotal.ToUInt64() * pageSize),
                CommitLimitBytes: checked(performanceInfo.CommitLimit.ToUInt64() * pageSize),
                AvailablePhysicalBytes: checked(performanceInfo.PhysicalAvailable.ToUInt64() * pageSize),
                TotalPhysicalBytes: checked(performanceInfo.PhysicalTotal.ToUInt64() * pageSize),
                PagesPerSecond: _pagesPerSecond.GetDoubleValue(),
                PageReadsPerSecond: _pageReadsPerSecond.GetDoubleValue(),
                PagefileUsagePercent: _pagefileUsage?.GetDoubleValue(),
                ReclaimableCacheBytes: checked(performanceInfo.SystemCache.ToUInt64() * pageSize));

            return ValueTask.FromResult(MemoryMetricSnapshotResult.Success(snapshot));
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or OverflowException)
        {
            return ValueTask.FromResult(MemoryMetricSnapshotResult.Failure(ex.Message));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pdhQuery.Dispose();
        _disposed = true;
    }
}
