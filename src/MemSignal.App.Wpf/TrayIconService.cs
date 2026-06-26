using System.Drawing;
using System.Windows.Forms;
using MemSignal.Infrastructure.Windows;

namespace MemSignal.App.Wpf;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly TrayIconRenderer _renderer;
    private Icon? _currentIcon;
    private string? _iconIdentity;
    private bool _disposed;

    public TrayIconService(Action openWindow, Action exitApplication, TrayIconRenderer? renderer = null)
    {
        _renderer = renderer ?? new TrayIconRenderer();
        _statusItem = new ToolStripMenuItem("--% · Data unavailable") { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.HandleCreated += (_, _) => NativeMethods.ApplySmallRoundedCorners(menu.Handle);
        menu.Items.Add("Open MemSignal", null, (_, _) => openWindow());
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exitApplication());

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Text = "MemSignal: data unavailable",
            Visible = false
        };
        _notifyIcon.DoubleClick += (_, _) => openWindow();
        Update(TrayPresentation.From(MemSignal.Application.MemoryPressureUpdate.Unknown("Waiting for data.")));
        _notifyIcon.Visible = true;
    }

    public void Update(TrayPresentation presentation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _notifyIcon.Text = TruncateToolTip(presentation.ToolTipText);
        _statusItem.Text = presentation.MenuStatusText;

        if (_iconIdentity == presentation.IconIdentity)
        {
            return;
        }

        var nextIcon = _renderer.Render(presentation);
        var previousIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        _iconIdentity = presentation.IconIdentity;
        previousIcon?.Dispose();
    }

    public void ShowBackgroundNotice()
    {
        _notifyIcon.ShowBalloonTip(
            4000,
            "MemSignal is still running",
            "Monitoring continues in the system tray. Use the tray menu and choose Exit to stop it.",
            ToolTipIcon.Info);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
        _currentIcon = null;
    }

    private static string TruncateToolTip(string value) => value.Length <= 63 ? value : value[..63];
}
