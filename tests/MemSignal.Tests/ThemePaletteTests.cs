using System.Windows.Media;
using MemSignal.App.Wpf;
using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class ThemePaletteTests
{
    [Theory]
    [InlineData(MemoryPressureClassification.Healthy, false, "#FF1F8F2E")]
    [InlineData(MemoryPressureClassification.Moderate, false, "#FFB77900")]
    [InlineData(MemoryPressureClassification.Elevated, false, "#FFC62828")]
    [InlineData(MemoryPressureClassification.Healthy, true, "#FF6CCB5F")]
    [InlineData(MemoryPressureClassification.Moderate, true, "#FFF9C74F")]
    [InlineData(MemoryPressureClassification.Elevated, true, "#FFFF6B6B")]
    public void StateBrushUsesThemePalette(
        MemoryPressureClassification classification,
        bool isDark,
        string expectedColor)
    {
        var brush = Assert.IsType<SolidColorBrush>(ThemePalette.StateBrush(classification, isDark));

        Assert.Equal(expectedColor, brush.Color.ToString());
    }
}
