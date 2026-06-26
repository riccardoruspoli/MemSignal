namespace MemSignal.Core;

public sealed record MemoryMetricSnapshotResult
{
    private MemoryMetricSnapshotResult(MemoryMetricSnapshot? snapshot, string? errorMessage)
    {
        Snapshot = snapshot;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess => Snapshot is not null;

    public MemoryMetricSnapshot? Snapshot { get; }

    public string? ErrorMessage { get; }

    public static MemoryMetricSnapshotResult Success(MemoryMetricSnapshot snapshot) => new(snapshot, null);

    public static MemoryMetricSnapshotResult Failure(string errorMessage) => new(null, errorMessage);
}
