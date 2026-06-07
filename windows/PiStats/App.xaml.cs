using System.IO;
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

        // Headless self-test: `dotnet run -- --dump` writes a report and exits.
        if (e.Args.Contains("--dump"))
        {
            try
            {
                var report = Core.Diagnostics.Dump();
                var outPath = Path.Combine(Path.GetTempPath(), "pistats-dump.txt");
                File.WriteAllText(outPath, report);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "pistats-dump.txt"),
                    "ERROR: " + ex);
            }
            Shutdown();
            return;
        }

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
