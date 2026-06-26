using MemSignal.Application;
using MemSignal.Core;

namespace MemSignal.App.Wpf;

public enum TrayVisualState
{
    Unknown,
    Normal,
    Warning,
    Critical
}

public sealed record TrayPresentation(
    string DisplayToken,
    string PercentageText,
    string ClassificationText,
    TrayVisualState VisualState,
    string ToolTipText,
    string MenuStatusText)
{
    public string IconIdentity => $"{DisplayToken}:{VisualState}";

    public static TrayPresentation From(MemoryPressureUpdate update)
    {
        if (!update.IsKnown || update.Result is null)
        {
            return new(
                "—",
                "--%",
                "Data unavailable",
                TrayVisualState.Unknown,
                "MemSignal: data unavailable",
                "--% · Data unavailable");
        }

        var percentage = Math.Clamp(
            (int)Math.Round(update.Result.SmoothedScore * 100, MidpointRounding.AwayFromZero),
            0,
            100);
        var classification = update.Result.Classification switch
        {
            MemoryPressureClassification.Healthy => ("Normal", TrayVisualState.Normal),
            MemoryPressureClassification.Moderate => ("Warning", TrayVisualState.Warning),
            MemoryPressureClassification.Elevated => ("Critical", TrayVisualState.Critical),
            _ => ("Data unavailable", TrayVisualState.Unknown)
        };
        var percentageText = $"{percentage}%";
        var token = percentage == 100 ? "99+" : percentage.ToString();

        return new(
            token,
            percentageText,
            classification.Item1,
            classification.Item2,
            $"MemSignal: {percentageText} — {classification.Item1}",
            $"{percentageText} · {classification.Item1}");
    }
}
