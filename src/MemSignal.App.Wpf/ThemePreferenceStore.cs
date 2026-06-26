using System.IO;
using System.Text.Json;

namespace MemSignal.App.Wpf;

public sealed class ThemePreferenceStore
{
    private readonly string _settingsPath;

    public ThemePreferenceStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemSignal",
            "settings.json");
    }

    public ThemePreference Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return ThemePreference.System;
            }

            var settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(_settingsPath));
            return Enum.TryParse<ThemePreference>(settings?.Theme, ignoreCase: true, out var preference)
                ? preference
                : ThemePreference.System;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or System.Security.SecurityException)
        {
            return ThemePreference.System;
        }
    }

    public bool TrySave(ThemePreference preference)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new ThemeSettings { Theme = preference.ToString() };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private sealed class ThemeSettings
    {
        public string? Theme { get; set; }
    }
}
