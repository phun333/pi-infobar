using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using H.NotifyIcon;
using PiStats.Assets;
using PiStats.Core;
using PiStats.Views;

namespace PiStats;

public partial class App : Application
{
    private TaskbarIcon? _tray;
    private PopoverWindow? _popover;
    private StatsEngine? _engine;
    private DispatcherTimer? _refreshTimer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Headless self-test: `dotnet run -- --dump` writes a report and exits.
        if (e.Args.Contains("--dump"))
        {
            try
            {
                var report = Diagnostics.Dump();
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "pistats-dump.txt"), report);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "pistats-dump.txt"), "ERROR: " + ex);
            }
            Shutdown();
            return;
        }

        // Render-to-PNG self-test: `dotnet run -- --shot` captures each tab and exits.
        if (e.Args.Contains("--shot"))
        {
            await CaptureShotsAsync();
            Shutdown();
            return;
        }

        _engine = new StatsEngine();
        _engine.PropertyChanged += OnEngineChanged;

        // Tray icon (the macOS NSStatusItem equivalent).
        _tray = new TaskbarIcon
        {
            ToolTipText = "Pi Stats",
            Icon = PiLogo.RenderTrayIcon(32, Colors.White),
        };
        _tray.TrayLeftMouseUp += (_, _) => TogglePopover();
        _tray.TrayRightMouseUp += (_, _) => ShowTrayMenu();

        _popover = new PopoverWindow(_engine);

        // Initial load + periodic refresh (macOS uses a 300s timer).
        await _engine.LoadAsync();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(300) };
        _refreshTimer.Tick += async (_, _) => await _engine.LoadAsync();
        _refreshTimer.Start();
    }

    private static async Task CaptureShotsAsync()
    {
        var engine = new StatsEngine();
        await engine.LoadAsync();

        var win = new PopoverWindow(engine)
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -3000,
            Top = -3000
        };
        win.Show();
        win.UpdateLayout();

        var dir = Path.GetTempPath();
        foreach (var tab in new[] { "Overview", "Languages", "Models", "Projects", "Usage" })
        {
            win.SelectTab(tab);
            win.RenderPng(Path.Combine(dir, $"pistats-{tab.ToLowerInvariant()}.png"));
        }
        win.Close();
    }

    private void OnEngineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_engine == null || _tray == null) return;
        Dispatcher.Invoke(() =>
        {
            var today = _engine.TodayCost;
            _tray.ToolTipText = _engine.Loading
                ? "Pi Stats — updating…"
                : $"Pi Stats — ${today:F2} today";
        });
    }

    private void TogglePopover()
    {
        if (_popover == null) return;
        if (_popover.IsVisible) _popover.HidePopover();
        else _popover.ShowPopover();
    }

    private void ShowTrayMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settings = new System.Windows.Controls.MenuItem { Header = "Settings…" };
        settings.Click += (_, _) => { /* TODO: settings window (Step 7) */ };

        var refresh = new System.Windows.Controls.MenuItem { Header = "Refresh" };
        refresh.Click += async (_, _) => { if (_engine != null) await _engine.LoadAsync(force: true); };

        var quit = new System.Windows.Controls.MenuItem { Header = "Quit" };
        quit.Click += (_, _) => Shutdown();

        menu.Items.Add(settings);
        menu.Items.Add(refresh);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(quit);
        menu.IsOpen = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _refreshTimer?.Stop();
        _tray?.Dispose();
        base.OnExit(e);
    }
}
