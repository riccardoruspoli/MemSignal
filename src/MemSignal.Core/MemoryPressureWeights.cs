namespace MemSignal.Core;

public sealed record MemoryPressureWeights(
    double Commit = 0.35,
    double Available = 0.25,
    double Paging = 0.20,
    double HardFault = 0.15,
    double Pagefile = 0.05);
