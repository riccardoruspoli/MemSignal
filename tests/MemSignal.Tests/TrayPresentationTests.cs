using MemSignal.App.Wpf;
using MemSignal.Application;
using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class TrayPresentationTests
{
    [Theory]
    [InlineData(0.624, "62", "62%")]
    [InlineData(0.625, "63", "63%")]
    [InlineData(0.999, "99+", "100%")]
    [InlineData(1.0, "99+", "100%")]
    public void KnownUpdateFormatsRoundedPercentage(double score, string token, string percentage)
    {
        var presentation = TrayPresentation.From(Known(score, MemoryPressureClassification.Healthy));

        Assert.Equal(token, presentation.DisplayToken);
        Assert.Equal(percentage, presentation.PercentageText);
        Assert.Contains(percentage, presentation.ToolTipText);
        Assert.Contains("Normal", presentation.ToolTipText);
        Assert.Contains("Normal", presentation.MenuStatusText);
    }

    [Theory]
    [InlineData(MemoryPressureClassification.Healthy, TrayVisualState.Normal, "Normal")]
    [InlineData(MemoryPressureClassification.Moderate, TrayVisualState.Warning, "Warning")]
    [InlineData(MemoryPressureClassification.Elevated, TrayVisualState.Critical, "Critical")]
    public void ClassificationMapsToAccessibleState(
        MemoryPressureClassification classification,
        TrayVisualState visualState,
        string text)
    {
        var presentation = TrayPresentation.From(Known(0.75, classification));

        Assert.Equal(visualState, presentation.VisualState);
        Assert.Equal(text, presentation.ClassificationText);
        Assert.Contains(text, presentation.ToolTipText);
    }

    [Fact]
    public void UnknownUpdateUsesNeutralUnavailableState()
    {
        var presentation = TrayPresentation.From(MemoryPressureUpdate.Unknown("failed"));

        Assert.Equal("—", presentation.DisplayToken);
        Assert.Equal("--%", presentation.PercentageText);
        Assert.Equal(TrayVisualState.Unknown, presentation.VisualState);
        Assert.Contains("unavailable", presentation.ToolTipText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IconIdentityChangesOnlyForTokenOrVisualState()
    {
        var first = TrayPresentation.From(Known(0.624, MemoryPressureClassification.Healthy));
        var same = TrayPresentation.From(Known(0.623, MemoryPressureClassification.Healthy));
        var changedState = TrayPresentation.From(Known(0.623, MemoryPressureClassification.Moderate));

        Assert.Equal(first.IconIdentity, same.IconIdentity);
        Assert.NotEqual(first.IconIdentity, changedState.IconIdentity);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    public void RendererProducesRequestedIconSize(int size)
    {
        var renderer = new TrayIconRenderer();
        var presentation = TrayPresentation.From(Known(0.63, MemoryPressureClassification.Moderate));

        using var icon = renderer.Render(presentation, size);

        Assert.Equal(size, icon.Width);
        Assert.Equal(size, icon.Height);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void RendererCanReplaceIconsRepeatedly()
    {
        var renderer = new TrayIconRenderer();

        for (var index = 0; index < 500; index++)
        {
            var classification = (MemoryPressureClassification)(index % 3);
            var presentation = TrayPresentation.From(Known(index % 101 / 100d, classification));
            using var icon = renderer.Render(presentation);
            Assert.NotEqual(IntPtr.Zero, icon.Handle);
        }
    }

    private static MemoryPressureUpdate Known(double score, MemoryPressureClassification classification)
    {
        var snapshot = new MemoryMetricSnapshot(DateTimeOffset.UtcNow, 1, 2, 1, 2, 0, 0, 0);
        var components = new PressureComponentValues(0, 0, 0, 0, 0);
        return MemoryPressureUpdate.Known(new MemoryPressureResult(snapshot, components, score, score, classification));
    }
}
