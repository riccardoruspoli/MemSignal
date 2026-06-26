using MemSignal.Core;

namespace MemSignal.Application;

public sealed record MemoryPressureUpdate
{
    private MemoryPressureUpdate(MemoryPressureResult? result, string? errorMessage)
    {
        Result = result;
        ErrorMessage = errorMessage;
    }

    public bool IsKnown => Result is not null;

    public MemoryPressureResult? Result { get; }

    public string? ErrorMessage { get; }

    public static MemoryPressureUpdate Known(MemoryPressureResult result) => new(result, null);

    public static MemoryPressureUpdate Unknown(string errorMessage) => new(null, errorMessage);
}
