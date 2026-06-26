using System.Windows.Media;
using MemSignal.Core;

namespace MemSignal.App.Wpf;

public static class ThemePalette
{
    public static Brush StateBrush(MemoryPressureClassification? classification, bool isDark)
    {
        var color = (classification, isDark) switch
        {
            (MemoryPressureClassification.Healthy, false) => "#1F8F2E",
            (MemoryPressureClassification.Moderate, false) => "#B77900",
            (MemoryPressureClassification.Elevated, false) => "#C62828",
            (_, false) => "#64748B",
            (MemoryPressureClassification.Healthy, true) => "#6CCB5F",
            (MemoryPressureClassification.Moderate, true) => "#F9C74F",
            (MemoryPressureClassification.Elevated, true) => "#FF6B6B",
            _ => "#A0A0A0"
        };

        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}
