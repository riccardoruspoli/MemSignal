using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using MemSignal.Application;

namespace MemSignal.App.Wpf;

public partial class MainWindow : Window
{
    private readonly MemoryPressureMonitorService _monitorService;
    private readonly MainViewModel _viewModel;
    private readonly ThemeManager _themeManager;
    private readonly Func<bool> _isExplicitShutdown;
    private readonly Action _hiddenByClose;

    public MainWindow(
        MemoryPressureMonitorService monitorService,
        MainViewModel viewModel,
        ThemeManager themeManager,
        Func<bool> isExplicitShutdown,
        Action hiddenByClose)
    {
        _monitorService = monitorService;
        _viewModel = viewModel;
        _themeManager = themeManager;
        _isExplicitShutdown = isExplicitShutdown;
        _hiddenByClose = hiddenByClose;

        InitializeComponent();
        DataContext = _viewModel;
        ApplyThemePresentation();

        Closing += OnClosing;
        StateChanged += OnStateChanged;
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExplicitShutdown())
        {
            e.Cancel = true;
            Hide();
            _hiddenByClose();
            return;
        }

        _themeManager.ThemeChanged -= OnThemeChanged;
        StateChanged -= OnStateChanged;
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await _monitorService.StopAsync();
        await _monitorService.StartAsync();
    }

    private void OnThemeClicked(object sender, RoutedEventArgs e)
    {
        var horizontalOffset = MainScrollViewer.HorizontalOffset;
        var verticalOffset = MainScrollViewer.VerticalOffset;

        _themeManager.Toggle();

        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            () =>
            {
                MainScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
                MainScrollViewer.ScrollToVerticalOffset(verticalOffset);
            });
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyThemePresentation();
    }

    private void ApplyThemePresentation()
    {
        _viewModel.ApplyTheme(_themeManager.IsDark);

        var actionName = _themeManager.IsDark ? "Use light theme" : "Use dark theme";
        ThemeIcon.Text = _themeManager.IsDark ? "\uE706" : "\uE708";
        ThemeLabel.Text = _themeManager.IsDark ? "Light" : "Dark";
        ThemeButton.ToolTip = actionName;
        AutomationProperties.SetName(ThemeButton, actionName);
    }
}
