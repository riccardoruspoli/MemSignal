using System.IO;

namespace MemSignal.App.Wpf;

public sealed class BackgroundNoticePreferenceStore
{
    private readonly string _path;

    public BackgroundNoticePreferenceStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemSignal",
            "background-notice-shown");
    }

    public bool HasBeenShown()
    {
        try
        {
            return File.Exists(_path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    public bool TryMarkShown()
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_path, "1");
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
