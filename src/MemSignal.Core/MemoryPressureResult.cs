namespace MemSignal.Core;

public sealed record MemoryPressureResult(
    MemoryMetricSnapshot Snapshot,
    PressureComponentValues Components,
    double CurrentScore,
    double SmoothedScore,
    MemoryPressureClassification Classification);
