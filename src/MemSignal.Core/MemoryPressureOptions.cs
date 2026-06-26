namespace MemSignal.Core;

public sealed record MemoryPressureOptions
{
    public static MemoryPressureOptions Default { get; } = new();

    public MemoryPressureWeights Weights { get; init; } = new();

    public MemoryPressureThresholds Thresholds { get; init; } = new();

    public double PagingPagesPerSecondLimit { get; init; } = 200;

    public double HardFaultPageReadsPerSecondLimit { get; init; } = 50;

    public double SmoothingAlpha { get; init; } = 0.2;

    public TimeSpan SamplingInterval { get; init; } = TimeSpan.FromSeconds(1);
}
