using System.IO;
using MemSignal.App.Wpf;
using Xunit;

namespace MemSignal.Tests;

public sealed class BackgroundNoticePreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"memory-pressure-notice-{Guid.NewGuid():N}");

    [Fact]
    public void MissingPreferenceHasNotBeenShown()
    {
        var store = new BackgroundNoticePreferenceStore(Path.Combine(_directory, "notice"));
        Assert.False(store.HasBeenShown());
    }

    [Fact]
    public void MarkShownPersistsThePreference()
    {
        var store = new BackgroundNoticePreferenceStore(Path.Combine(_directory, "notice"));

        Assert.True(store.TryMarkShown());
        Assert.True(store.HasBeenShown());
    }

    [Fact]
    public void WriteFailureDoesNotThrow()
    {
        Directory.CreateDirectory(_directory);
        var store = new BackgroundNoticePreferenceStore(_directory);
        Assert.False(store.TryMarkShown());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
