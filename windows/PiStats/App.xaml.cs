using System.Windows;
using System.Windows.Media;
using H.NotifyIcon;
using PiStats.Assets;
using PiStats.Views;

namespace PiStats;

public partial class App : Application
{
    private TaskbarIcon? _tray;
    private PopoverWindow? _popover;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tray icon (the macOS NSStatusItem equivalent).
        _tray = new TaskbarIcon
        {
            ToolTipText = "Pi Stats",
            Icon = PiLogo.RenderTrayIcon(32, Colors.White),
        };
        _tray.TrayLeftMouseUp += (_, _) => TogglePopover();
        _tray.TrayRightMouseUp += (_, _) => ShowTrayMenu();

        _popover = new PopoverWindow();
    }

    private void TogglePopover()
    {
        if (_popover == null) return;
        if (_popover.IsVisible)
            _popover.HidePopover();
        else
            _popover.ShowPopover();
    }

    private void ShowTrayMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settings = new System.Windows.Controls.MenuItem { Header = "Settings…" };
        settings.Click += (_, _) => { /* TODO: settings window */ };

        var quit = new System.Windows.Controls.MenuItem { Header = "Quit" };
        quit.Click += (_, _) => Shutdown();

        menu.Items.Add(settings);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(quit);
        menu.IsOpen = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
