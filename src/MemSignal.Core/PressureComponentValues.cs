namespace MemSignal.Core;

public sealed record PressureComponentValues(
    double CommitPressure,
    double AvailablePressure,
    double PagingPressure,
    double HardFaultPressure,
    double? PagefilePressure);
