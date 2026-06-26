using System.Windows;
using MemSignal.Application;
using MemSignal.Bootstrapper.Windows;

namespace MemSignal.App.Wpf;

public partial class App : System.Windows.Application
{
    private WindowsMemoryPressureRuntime? _runtime;
    private ThemeManager? _themeManager;
    private MainViewModel? _viewModel;
    private MainWindow? _mainWindow;
    private TrayIconService? _trayIcon;
    private readonly BackgroundNoticePreferenceStore _noticeStore = new();
    private bool _isExplicitShutdown;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _themeManager = new ThemeManager(this, new ThemePreferenceStore());
        _themeManager.Initialize();

        _runtime = WindowsMemoryPressureRuntime.Create();

        _viewModel = new MainViewModel(_runtime.Options);
        _mainWindow = new MainWindow(
            _runtime.MonitorService,
            _viewModel,
            _themeManager,
            () => _isExplicitShutdown,
            OnWindowHiddenByClose);
        MainWindow = _mainWindow;

        _trayIcon = new TrayIconService(RestoreMainWindow, ExitFromTray);
        _runtime.MonitorService.UpdateProduced += OnUpdateProduced;
        _mainWindow.Show();
        _ = StartMonitoringAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.MonitorService.UpdateProduced -= OnUpdateProduced;
            await _runtime.MonitorService.StopAsync();
            await _runtime.DisposeAsync();
        }

        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private async Task StartMonitoringAsync()
    {
        if (_runtime is not null)
        {
            await _runtime.MonitorService.StartAsync();
        }
    }

    private void OnUpdateProduced(object? sender, MemoryPressureUpdate update)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _viewModel?.Apply(update);
            _trayIcon?.Update(TrayPresentation.From(update));
        });
    }

    private void RestoreMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }

    private void OnWindowHiddenByClose()
    {
        if (_noticeStore.HasBeenShown())
        {
            return;
        }

        _trayIcon?.ShowBackgroundNotice();
        _noticeStore.TryMarkShown();
    }

    private async void ExitFromTray()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _isExplicitShutdown = true;

        if (_runtime is not null)
        {
            _runtime.MonitorService.UpdateProduced -= OnUpdateProduced;
            await _runtime.MonitorService.StopAsync();
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
        _mainWindow?.Close();
        Shutdown();
    }
}
