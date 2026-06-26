using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class PressureScenarioTests
{
    [Fact]
    public void NormalSyntheticLoad_ClassifiesHealthy()
    {
        var result = new MemoryPressureCalculator().Calculate(new MemoryMetricSnapshot(
            DateTimeOffset.UnixEpoch,
            CommittedBytes: 30,
            CommitLimitBytes: 100,
            AvailablePhysicalBytes: 70,
            TotalPhysicalBytes: 100,
            PagesPerSecond: 5,
            PageReadsPerSecond: 0,
            PagefileUsagePercent: 2,
            ReclaimableCacheBytes: 40));

        Assert.Equal(MemoryPressureClassification.Healthy, result.Classification);
    }

    [Fact]
    public void ModerateSyntheticPressure_ClassifiesModerate()
    {
        var result = new MemoryPressureCalculator().Calculate(new MemoryMetricSnapshot(
            DateTimeOffset.UnixEpoch,
            CommittedBytes: 85,
            CommitLimitBytes: 100,
            AvailablePhysicalBytes: 5,
            TotalPhysicalBytes: 100,
            PagesPerSecond: 160,
            PageReadsPerSecond: 40,
            PagefileUsagePercent: 70));

        Assert.Equal(MemoryPressureClassification.Moderate, result.Classification);
    }

    [Fact]
    public void HighSyntheticPressure_ClassifiesElevated()
    {
        var result = new MemoryPressureCalculator().Calculate(new MemoryMetricSnapshot(
            DateTimeOffset.UnixEpoch,
            CommittedBytes: 95,
            CommitLimitBytes: 100,
            AvailablePhysicalBytes: 5,
            TotalPhysicalBytes: 100,
            PagesPerSecond: 220,
            PageReadsPerSecond: 75,
            PagefileUsagePercent: 80));

        Assert.Equal(MemoryPressureClassification.Elevated, result.Classification);
    }
}
