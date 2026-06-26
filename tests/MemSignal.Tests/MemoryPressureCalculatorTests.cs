using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class MemoryPressureCalculatorTests
{
    [Theory]
    [InlineData(-0.1, 0.9)]
    [InlineData(0.8, 1.1)]
    [InlineData(0.9, 0.8)]
    [InlineData(double.NaN, 0.9)]
    [InlineData(0.8, double.PositiveInfinity)]
    public void Constructor_RejectsInvalidClassificationThresholds(double moderate, double elevated)
    {
        var options = MemoryPressureOptions.Default with
        {
            Thresholds = new MemoryPressureThresholds(moderate, elevated)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryPressureCalculator(options));
    }

    [Fact]
    public void Normalize_UsesConfiguredFormulas()
    {
        var calculator = new MemoryPressureCalculator();
        var snapshot = Snapshot(
            committedBytes: 50,
            commitLimitBytes: 100,
            availablePhysicalBytes: 25,
            totalPhysicalBytes: 100,
            pagesPerSecond: 100,
            pageReadsPerSecond: 25,
            pagefileUsagePercent: 40);

        var components = calculator.Normalize(snapshot);

        Assert.Equal(0.5, components.CommitPressure, 6);
        Assert.Equal(0.75, components.AvailablePressure, 6);
        Assert.Equal(0.5, components.PagingPressure, 6);
        Assert.Equal(0.5, components.HardFaultPressure, 6);
        Assert.Equal(0.4, Assert.IsType<double>(components.PagefilePressure), 6);
    }

    [Fact]
    public void Normalize_ClampsComponents()
    {
        var calculator = new MemoryPressureCalculator();
        var snapshot = Snapshot(
            committedBytes: 200,
            commitLimitBytes: 100,
            availablePhysicalBytes: 200,
            totalPhysicalBytes: 100,
            pagesPerSecond: 400,
            pageReadsPerSecond: 100,
            pagefileUsagePercent: 200);

        var components = calculator.Normalize(snapshot);

        Assert.Equal(1, components.CommitPressure);
        Assert.Equal(0, components.AvailablePressure);
        Assert.Equal(1, components.PagingPressure);
        Assert.Equal(1, components.HardFaultPressure);
        Assert.Equal(1, components.PagefilePressure);
    }

    [Fact]
    public void Calculate_UsesWeightedScore()
    {
        var calculator = new MemoryPressureCalculator();
        var result = calculator.Calculate(Snapshot(
            committedBytes: 50,
            commitLimitBytes: 100,
            availablePhysicalBytes: 50,
            totalPhysicalBytes: 100,
            pagesPerSecond: 100,
            pageReadsPerSecond: 25,
            pagefileUsagePercent: 50));

        Assert.Equal(0.5, result.CurrentScore, 6);
        Assert.Equal(0.5, result.SmoothedScore, 6);
    }

    [Fact]
    public void Calculate_RenormalizesWeightsWhenPagefileMetricIsUnavailable()
    {
        var calculator = new MemoryPressureCalculator();
        var result = calculator.Calculate(Snapshot(
            committedBytes: 50,
            commitLimitBytes: 100,
            availablePhysicalBytes: 50,
            totalPhysicalBytes: 100,
            pagesPerSecond: 100,
            pageReadsPerSecond: 25,
            pagefileUsagePercent: null));

        Assert.Null(result.Components.PagefilePressure);
        Assert.Equal(0.5, result.CurrentScore, 6);
        Assert.Equal(0.5, result.SmoothedScore, 6);
    }

    [Fact]
    public void Calculate_InitializesEmaFromFirstSample()
    {
        var calculator = new MemoryPressureCalculator();
        var result = calculator.Calculate(Snapshot(
            committedBytes: 40,
            commitLimitBytes: 100,
            availablePhysicalBytes: 60,
            totalPhysicalBytes: 100,
            pagesPerSecond: 80,
            pageReadsPerSecond: 20,
            pagefileUsagePercent: 40));

        Assert.Equal(result.CurrentScore, result.SmoothedScore, 6);
    }

    [Fact]
    public void Calculate_UpdatesEmaForSubsequentSamples()
    {
        var calculator = new MemoryPressureCalculator();
        var first = calculator.Calculate(Snapshot(
            committedBytes: 0,
            commitLimitBytes: 100,
            availablePhysicalBytes: 100,
            totalPhysicalBytes: 100,
            pagesPerSecond: 0,
            pageReadsPerSecond: 0,
            pagefileUsagePercent: 0));
        var second = calculator.Calculate(Snapshot(
            committedBytes: 100,
            commitLimitBytes: 100,
            availablePhysicalBytes: 0,
            totalPhysicalBytes: 100,
            pagesPerSecond: 200,
            pageReadsPerSecond: 50,
            pagefileUsagePercent: 100));

        Assert.Equal(0, first.SmoothedScore);
        Assert.Equal(1, second.CurrentScore);
        Assert.Equal(0.2, second.SmoothedScore, 6);
    }

    [Theory]
    [InlineData(0.799, MemoryPressureClassification.Healthy)]
    [InlineData(0.800, MemoryPressureClassification.Moderate)]
    [InlineData(0.899, MemoryPressureClassification.Moderate)]
    [InlineData(0.900, MemoryPressureClassification.Elevated)]
    [InlineData(1.000, MemoryPressureClassification.Elevated)]
    public void Classify_UsesExpectedBoundaries(double score, MemoryPressureClassification expected)
    {
        var calculator = new MemoryPressureCalculator();

        Assert.Equal(expected, calculator.Classify(score));
    }

    [Fact]
    public void Calculate_HighRamOccupancyAloneDoesNotForceElevatedPressure()
    {
        var calculator = new MemoryPressureCalculator();
        var result = calculator.Calculate(new MemoryMetricSnapshot(
            Timestamp: DateTimeOffset.UnixEpoch,
            CommittedBytes: 40,
            CommitLimitBytes: 100,
            AvailablePhysicalBytes: 5,
            TotalPhysicalBytes: 100,
            PagesPerSecond: 0,
            PageReadsPerSecond: 0,
            PagefileUsagePercent: 0,
            ReclaimableCacheBytes: 90));

        Assert.NotEqual(MemoryPressureClassification.Elevated, result.Classification);
    }

    private static MemoryMetricSnapshot Snapshot(
        ulong committedBytes,
        ulong commitLimitBytes,
        ulong availablePhysicalBytes,
        ulong totalPhysicalBytes,
        double pagesPerSecond,
        double pageReadsPerSecond,
        double? pagefileUsagePercent)
    {
        return new MemoryMetricSnapshot(
            DateTimeOffset.UnixEpoch,
            committedBytes,
            commitLimitBytes,
            availablePhysicalBytes,
            totalPhysicalBytes,
            pagesPerSecond,
            pageReadsPerSecond,
            pagefileUsagePercent);
    }
}
