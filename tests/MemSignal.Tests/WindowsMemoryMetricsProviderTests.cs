using MemSignal.Core;
using MemSignal.Infrastructure.Windows;
using Xunit;

namespace MemSignal.Tests;

public sealed class WindowsMemoryMetricsProviderTests
{
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SampleAsync_ReturnsUsableWindowsMetricSnapshot()
    {
        using var provider = new WindowsMemoryMetricsProvider();

        var result = await provider.SampleAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Snapshot);
        Assert.True(result.Snapshot.CommitLimitBytes > 0);
        Assert.True(result.Snapshot.TotalPhysicalBytes > 0);
        Assert.InRange(result.Snapshot.CommittedBytes, 1UL, result.Snapshot.CommitLimitBytes);
        Assert.InRange(result.Snapshot.AvailablePhysicalBytes, 0UL, result.Snapshot.TotalPhysicalBytes);
        Assert.True(result.Snapshot.PagesPerSecond >= 0);
        Assert.True(result.Snapshot.PageReadsPerSecond >= 0);
        if (result.Snapshot.PagefileUsagePercent is double pagefileUsagePercent)
        {
            Assert.InRange(pagefileUsagePercent, 0, 100);
        }

        var pressure = new MemoryPressureCalculator().Calculate(result.Snapshot);

        Assert.InRange(pressure.CurrentScore, 0, 1);
        Assert.InRange(pressure.SmoothedScore, 0, 1);
    }
}
