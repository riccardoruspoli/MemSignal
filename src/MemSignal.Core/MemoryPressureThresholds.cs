namespace MemSignal.Core;

public sealed record MemoryPressureThresholds(
    double Moderate = 0.80,
    double Elevated = 0.90);
