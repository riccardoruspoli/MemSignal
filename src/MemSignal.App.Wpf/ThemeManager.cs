using System.Windows;
using Microsoft.Win32;

namespace MemSignal.App.Wpf;

public sealed class ThemeManager
{
    private static readonly Uri LightThemeUri = new("Themes/Light.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeUri = new("Themes/Dark.xaml", UriKind.Relative);

    private readonly System.Windows.Application _application;
    private readonly ThemePreferenceStore _preferenceStore;
    private ResourceDictionary? _activeDictionary;

    public ThemeManager(System.Windows.Application application, ThemePreferenceStore preferenceStore)
    {
        _application = application;
        _preferenceStore = preferenceStore;
    }

    public event EventHandler? ThemeChanged;

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public bool IsDark { get; private set; }

    public void Initialize()
    {
        Apply(_preferenceStore.Load(), persist: false);
    }

    public void Toggle()
    {
        Apply(IsDark ? ThemePreference.Light : ThemePreference.Dark, persist: true);
    }

    public void Apply(ThemePreference preference, bool persist)
    {
        Preference = preference;
        IsDark = ResolveIsDark(preference, IsWindowsDarkMode());

        ApplyNativeTheme(preference);
        ApplyResourceDictionary(IsDark ? DarkThemeUri : LightThemeUri);

        if (persist && preference != ThemePreference.System)
        {
            _preferenceStore.TrySave(preference);
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    public static bool ResolveIsDark(ThemePreference preference, bool windowsIsDark) =>
        preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => windowsIsDark
        };

    private void ApplyResourceDictionary(Uri source)
    {
        var dictionaries = _application.Resources.MergedDictionaries;
        if (_activeDictionary is not null)
        {
            dictionaries.Remove(_activeDictionary);
        }

        _activeDictionary = new ResourceDictionary { Source = source };
        dictionaries.Add(_activeDictionary);
    }

#pragma warning disable WPF0001
    private void ApplyNativeTheme(ThemePreference preference)
    {
        _application.ThemeMode = preference switch
        {
            ThemePreference.Light => ThemeMode.Light,
            ThemePreference.Dark => ThemeMode.Dark,
            _ => ThemeMode.System
        };
    }
#pragma warning restore WPF0001
}
