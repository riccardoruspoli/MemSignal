namespace MemSignal.Core;

public sealed class MemoryPressureCalculator
{
    private readonly MemoryPressureOptions _options;
    private double? _previousSmoothedScore;

    public MemoryPressureCalculator(MemoryPressureOptions? options = null)
    {
        _options = options ?? MemoryPressureOptions.Default;
        ValidateOptions(_options);
    }

    public MemoryPressureResult Calculate(MemoryMetricSnapshot snapshot)
    {
        var components = Normalize(snapshot);
        var weightedScore =
            _options.Weights.Commit * components.CommitPressure
            + _options.Weights.Available * components.AvailablePressure
            + _options.Weights.Paging * components.PagingPressure
            + _options.Weights.HardFault * components.HardFaultPressure;
        var includedWeight =
            _options.Weights.Commit
            + _options.Weights.Available
            + _options.Weights.Paging
            + _options.Weights.HardFault;

        if (components.PagefilePressure is double pagefilePressure)
        {
            weightedScore += _options.Weights.Pagefile * pagefilePressure;
            includedWeight += _options.Weights.Pagefile;
        }

        var currentScore = Clamp01(weightedScore / includedWeight);

        var smoothedScore = _previousSmoothedScore is null
            ? currentScore
            : _options.SmoothingAlpha * currentScore + (1 - _options.SmoothingAlpha) * _previousSmoothedScore.Value;

        smoothedScore = Clamp01(smoothedScore);
        _previousSmoothedScore = smoothedScore;

        return new MemoryPressureResult(
            snapshot,
            components,
            currentScore,
            smoothedScore,
            Classify(smoothedScore));
    }

    public PressureComponentValues Normalize(MemoryMetricSnapshot snapshot)
    {
        if (snapshot.CommitLimitBytes == 0)
        {
            throw new ArgumentException("Commit limit must be greater than zero.", nameof(snapshot));
        }

        if (snapshot.TotalPhysicalBytes == 0)
        {
            throw new ArgumentException("Total physical memory must be greater than zero.", nameof(snapshot));
        }

        return new PressureComponentValues(
            CommitPressure: Clamp01((double)snapshot.CommittedBytes / snapshot.CommitLimitBytes),
            AvailablePressure: Clamp01(1 - (double)snapshot.AvailablePhysicalBytes / snapshot.TotalPhysicalBytes),
            PagingPressure: Clamp01(snapshot.PagesPerSecond / _options.PagingPagesPerSecondLimit),
            HardFaultPressure: Clamp01(snapshot.PageReadsPerSecond / _options.HardFaultPageReadsPerSecondLimit),
            PagefilePressure: snapshot.PagefileUsagePercent is double pagefileUsagePercent
                ? Clamp01(pagefileUsagePercent / 100)
                : null);
    }

    public MemoryPressureClassification Classify(double smoothedScore)
    {
        var score = Clamp01(smoothedScore);

        if (score >= _options.Thresholds.Elevated)
        {
            return MemoryPressureClassification.Elevated;
        }

        if (score >= _options.Thresholds.Moderate)
        {
            return MemoryPressureClassification.Moderate;
        }

        return MemoryPressureClassification.Healthy;
    }

    public static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static void ValidateOptions(MemoryPressureOptions options)
    {
        if (options.PagingPagesPerSecondLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Paging threshold must be greater than zero.");
        }

        if (options.HardFaultPageReadsPerSecondLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Hard fault threshold must be greater than zero.");
        }

        if (options.SmoothingAlpha is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Smoothing alpha must be between zero and one.");
        }

        if (!double.IsFinite(options.Thresholds.Moderate)
            || options.Thresholds.Moderate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Moderate threshold must be between zero and one.");
        }

        if (!double.IsFinite(options.Thresholds.Elevated)
            || options.Thresholds.Elevated is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Elevated threshold must be between zero and one.");
        }

        if (options.Thresholds.Moderate > options.Thresholds.Elevated)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Moderate threshold must not exceed the elevated threshold.");
        }
    }
}
