using MemSignal.App.Wpf;
using MemSignal.Core;
using Xunit;

namespace MemSignal.Tests;

public sealed class ThresholdBarTests
{
    [Fact]
    public void NormalizeThresholds_PreservesValidConfiguredValues()
    {
        var thresholds = ThresholdBar.NormalizeThresholds(0.75, 0.88);

        Assert.Equal(0.75, thresholds.Moderate);
        Assert.Equal(0.88, thresholds.Elevated);
    }

    [Theory]
    [InlineData(-1, 2, 0, 1)]
    [InlineData(0.9, 0.8, 0.9, 0.9)]
    [InlineData(double.NaN, double.PositiveInfinity, 0, 0)]
    public void NormalizeThresholds_ProducesRenderableOrderedValues(
        double moderate,
        double elevated,
        double expectedModerate,
        double expectedElevated)
    {
        var thresholds = ThresholdBar.NormalizeThresholds(moderate, elevated);

        Assert.Equal(expectedModerate, thresholds.Moderate);
        Assert.Equal(expectedElevated, thresholds.Elevated);
    }

    [Fact]
    public void ViewModel_ExposesConfiguredCalculatorThresholds()
    {
        var options = MemoryPressureOptions.Default with
        {
            Thresholds = new MemoryPressureThresholds(0.72, 0.86)
        };

        var viewModel = new MainViewModel(options);

        Assert.Equal(options.Thresholds.Moderate, viewModel.ModerateThreshold);
        Assert.Equal(options.Thresholds.Elevated, viewModel.ElevatedThreshold);
    }
}
