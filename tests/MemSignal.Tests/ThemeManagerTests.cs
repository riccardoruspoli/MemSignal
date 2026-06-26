using MemSignal.App.Wpf;
using Xunit;

namespace MemSignal.Tests;

public sealed class ThemeManagerTests
{
    [Theory]
    [InlineData(ThemePreference.System, false, false)]
    [InlineData(ThemePreference.System, true, true)]
    [InlineData(ThemePreference.Light, true, false)]
    [InlineData(ThemePreference.Dark, false, true)]
    public void EffectiveThemeRespectsPreference(
        ThemePreference preference,
        bool windowsIsDark,
        bool expectedIsDark)
    {
        Assert.Equal(expectedIsDark, ThemeManager.ResolveIsDark(preference, windowsIsDark));
    }
}
