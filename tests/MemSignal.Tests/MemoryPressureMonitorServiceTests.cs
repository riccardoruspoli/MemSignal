using MemSignal.Abstractions;
using MemSignal.Application;
using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class MemoryPressureMonitorServiceTests
{
    [Fact]
    public async Task StartAsync_PublishesUnknownWhenProviderFails()
    {
        var provider = new FakeMemoryMetricsProvider(MemoryMetricSnapshotResult.Failure("counter unavailable"));
        var service = new MemoryPressureMonitorService(provider, new MemoryPressureCalculator());
        var completion = new TaskCompletionSource<MemoryPressureUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);

        service.UpdateProduced += (_, update) => completion.TrySetResult(update);

        await service.StartAsync(TestContext.Current.CancellationToken);
        var update = await completion.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await service.StopAsync();

        Assert.False(update.IsKnown);
        Assert.Equal("counter unavailable", update.ErrorMessage);
    }

    [Fact]
    public async Task StartAsync_PublishesKnownPressureResult()
    {
        var provider = new FakeMemoryMetricsProvider(MemoryMetricSnapshotResult.Success(new MemoryMetricSnapshot(
            DateTimeOffset.UnixEpoch,
            CommittedBytes: 50,
            CommitLimitBytes: 100,
            AvailablePhysicalBytes: 50,
            TotalPhysicalBytes: 100,
            PagesPerSecond: 0,
            PageReadsPerSecond: 0,
            PagefileUsagePercent: 0)));
        var service = new MemoryPressureMonitorService(provider, new MemoryPressureCalculator());
        var completion = new TaskCompletionSource<MemoryPressureUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);

        service.UpdateProduced += (_, update) => completion.TrySetResult(update);

        await service.StartAsync(TestContext.Current.CancellationToken);
        var update = await completion.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await service.StopAsync();

        Assert.True(update.IsKnown);
        Assert.NotNull(update.Result);
    }

    private sealed class FakeMemoryMetricsProvider : IMemoryMetricsProvider
    {
        private readonly MemoryMetricSnapshotResult _result;

        public FakeMemoryMetricsProvider(MemoryMetricSnapshotResult result)
        {
            _result = result;
        }

        public ValueTask<MemoryMetricSnapshotResult> SampleAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_result);
        }
    }
}
