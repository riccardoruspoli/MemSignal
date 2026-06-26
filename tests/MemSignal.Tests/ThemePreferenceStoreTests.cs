using System.IO;
using MemSignal.App.Wpf;
using Xunit;

namespace MemSignal.Tests;

public sealed class ThemePreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"memory-pressure-theme-{Guid.NewGuid():N}");

    [Fact]
    public void LoadReturnsSystemWhenSettingsAreMissing()
    {
        var store = new ThemePreferenceStore(Path.Combine(_directory, "settings.json"));

        Assert.Equal(ThemePreference.System, store.Load());
    }

    [Fact]
    public void LoadReturnsSystemWhenSettingsAreMalformed()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{not-json");

        var store = new ThemePreferenceStore(path);

        Assert.Equal(ThemePreference.System, store.Load());
    }

    [Theory]
    [InlineData(ThemePreference.Light)]
    [InlineData(ThemePreference.Dark)]
    public void SavedExplicitPreferenceCanBeLoaded(ThemePreference preference)
    {
        var store = new ThemePreferenceStore(Path.Combine(_directory, "settings.json"));

        Assert.True(store.TrySave(preference));
        Assert.Equal(preference, store.Load());
    }

    [Fact]
    public void SaveFailureIsReportedWithoutThrowing()
    {
        Directory.CreateDirectory(_directory);
        var store = new ThemePreferenceStore(_directory);

        Assert.False(store.TrySave(ThemePreference.Dark));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
