using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using MemSignal.Application;
using MemSignal.Core;

namespace MemSignal.App.Wpf;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private MemoryPressureClassification? _classification;
    private bool _isDarkTheme;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(MemoryPressureOptions? options = null)
    {
        var thresholds = (options ?? MemoryPressureOptions.Default).Thresholds;
        ModerateThreshold = thresholds.Moderate;
        ElevatedThreshold = thresholds.Elevated;
    }

    public double ModerateThreshold { get; }

    public double ElevatedThreshold { get; }

    public string ClassificationText { get; private set; } = "Unknown";

    public string ScoreText { get; private set; } = "--%";

    public double ScorePercent { get; private set; }

    public Brush StateBrush { get; private set; } = ThemePalette.StateBrush(null, false);

    public string StatusMessage { get; private set; } = "Waiting for the first memory metric sample.";

    public string LastUpdatedText { get; private set; } = "Updated every 1s";

    public IReadOnlyList<MetricRowViewModel> ComponentRows { get; private set; } = Array.Empty<MetricRowViewModel>();

    public IReadOnlyList<MetricRowViewModel> RawMetricRows { get; private set; } = Array.Empty<MetricRowViewModel>();

    public void Apply(MemoryPressureUpdate update)
    {
        if (!update.IsKnown || update.Result is null)
        {
            ApplyUnknown(update.ErrorMessage ?? "Required memory metrics are unavailable.");
            return;
        }

        var result = update.Result;
        _classification = result.Classification;

        ClassificationText = DisplayNameFor(result.Classification);
        ScorePercent = Math.Round(result.SmoothedScore * 100, 1);
        ScoreText = ScorePercent.ToString("0", CultureInfo.InvariantCulture) + "%";
        StateBrush = ThemePalette.StateBrush(_classification, _isDarkTheme);
        StatusMessage = StatusMessageFor(result.Classification);
        LastUpdatedText = $"Last updated: {result.Snapshot.Timestamp.LocalDateTime:G}";
        ComponentRows = BuildComponentRows(result);
        RawMetricRows = BuildRawMetricRows(result.Snapshot);

        NotifyAll();
    }

    private void ApplyUnknown(string message)
    {
        _classification = null;
        ClassificationText = "Unknown";
        ScoreText = "--%";
        ScorePercent = 0;
        StateBrush = ThemePalette.StateBrush(_classification, _isDarkTheme);
        StatusMessage = message;
        LastUpdatedText = "Metric collection failed";
        ComponentRows = Array.Empty<MetricRowViewModel>();
        RawMetricRows = Array.Empty<MetricRowViewModel>();

        NotifyAll();
    }

    public void ApplyTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        StateBrush = ThemePalette.StateBrush(_classification, _isDarkTheme);
        OnPropertyChanged(nameof(StateBrush));
    }

    private static IReadOnlyList<MetricRowViewModel> BuildComponentRows(MemoryPressureResult result)
    {
        return
        [
            new("Commit pressure", FormatRatio(result.Components.CommitPressure)),
            new("Available pressure", FormatRatio(result.Components.AvailablePressure)),
            new("Paging pressure", FormatRatio(result.Components.PagingPressure)),
            new("Hard fault pressure", FormatRatio(result.Components.HardFaultPressure)),
            new("Pagefile pressure", result.Components.PagefilePressure is double pagefilePressure
                ? FormatRatio(pagefilePressure)
                : "Unavailable"),
            new("Current score", FormatRatio(result.CurrentScore)),
            new("Smoothed score", FormatRatio(result.SmoothedScore)),
            new("Classification", result.Classification.ToString())
        ];
    }

    private static IReadOnlyList<MetricRowViewModel> BuildRawMetricRows(MemoryMetricSnapshot snapshot)
    {
        return
        [
            new("Committed bytes", FormatBytes(snapshot.CommittedBytes)),
            new("Commit limit", FormatBytes(snapshot.CommitLimitBytes)),
            new("Available physical memory", FormatBytes(snapshot.AvailablePhysicalBytes)),
            new("Total physical memory", FormatBytes(snapshot.TotalPhysicalBytes)),
            new("Pages/sec", snapshot.PagesPerSecond.ToString("0.0", CultureInfo.InvariantCulture)),
            new("Page reads/sec", snapshot.PageReadsPerSecond.ToString("0.0", CultureInfo.InvariantCulture)),
            new("Pagefile usage", snapshot.PagefileUsagePercent is double pagefileUsagePercent
                ? pagefileUsagePercent.ToString("0.0' %'", CultureInfo.InvariantCulture)
                : "Unavailable"),
            new("Reclaimable cache", snapshot.ReclaimableCacheBytes is null ? "Unavailable" : FormatBytes(snapshot.ReclaimableCacheBytes.Value))
        ];
    }

    private static string FormatRatio(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);

    private static string FormatBytes(ulong bytes)
    {
        const double gib = 1024d * 1024d * 1024d;
        return (bytes / gib).ToString("0.00' GB'", CultureInfo.InvariantCulture);
    }

    private static string DisplayNameFor(MemoryPressureClassification classification)
    {
        return classification switch
        {
            MemoryPressureClassification.Healthy => "Normal",
            MemoryPressureClassification.Moderate => "Warning",
            MemoryPressureClassification.Elevated => "Critical",
            _ => "Unknown"
        };
    }

    private static string StatusMessageFor(MemoryPressureClassification classification)
    {
        return classification switch
        {
            MemoryPressureClassification.Healthy => "Memory pressure is within normal range.",
            MemoryPressureClassification.Moderate => "Memory pressure is elevated but still within the warning range.",
            MemoryPressureClassification.Elevated => "Memory pressure is high. Consider closing memory-intensive apps.",
            _ => "Waiting for the first memory metric sample."
        };
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(ClassificationText));
        OnPropertyChanged(nameof(ScoreText));
        OnPropertyChanged(nameof(ScorePercent));
        OnPropertyChanged(nameof(StateBrush));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(LastUpdatedText));
        OnPropertyChanged(nameof(ComponentRows));
        OnPropertyChanged(nameof(RawMetricRows));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
