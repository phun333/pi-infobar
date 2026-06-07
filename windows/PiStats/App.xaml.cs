using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using H.NotifyIcon;
using PiStats.Assets;
using PiStats.Core;
using PiStats.Services;
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

        // Generate the app icon: `dotnet run -- --makeicon <path>` then exit.
        if (e.Args.Contains("--makeicon"))
        {
            var idx = Array.IndexOf(e.Args, "--makeicon");
            var outPath = idx >= 0 && idx + 1 < e.Args.Length
                ? e.Args[idx + 1]
                : Path.Combine(Path.GetTempPath(), "AppIcon.ico");
            PiLogo.WriteIcoFile(outPath);
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

        // Tray icon (the macOS NSStatusItem equivalent). Pick a color that
        // stays visible: dark glyph on a light taskbar, white on a dark one.
        var iconColor = SystemTheme.IsLightTaskbar
            ? Color.FromRgb(0x20, 0x20, 0x20)
            : Colors.White;
        _tray = new TaskbarIcon
        {
            ToolTipText = "Pi Stats",
            Icon = PiLogo.RenderTrayIcon(32, iconColor),
        };
        _tray.TrayLeftMouseUp += (_, _) => TogglePopover();
        _tray.TrayRightMouseUp += (_, _) => ShowTrayMenu();
        _tray.ForceCreate(); // register the Win32 icon (required when built in code)

        _popover = new PopoverWindow(_engine)
        {
            OnSettings = () => SettingsWindow.Open()
        };

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

        var settings = new SettingsWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -3000,
            Top = -3000
        };
        settings.Show();
        settings.UpdateLayout();
        string[] paneNames = { "menubar", "general", "remote", "about" };
        for (int i = 0; i < paneNames.Length; i++)
        {
            settings.SelectPane(i);
            settings.RenderPng(Path.Combine(dir, $"pistats-settings-{paneNames[i]}.png"));
        }
        settings.Close();
    }

    private void OnEngineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_engine == null || _tray == null) return;
        Dispatcher.Invoke(() =>
        {
            _tray.ToolTipText = _engine.Loading
                ? "Pi Stats — updating…"
                : $"Pi Stats — {MetricText()}";
        });
    }

    private string MetricText()
    {
        if (_engine == null) return "";
        return SettingsStore.Shared.MenuBarMetric switch
        {
            MenuBarMetric.TodayCost => $"${_engine.TodayCost:F2} today",
            MenuBarMetric.TotalCost => $"{Fmt.Money(_engine.TotalCostAll)} total",
            MenuBarMetric.TodayLines => $"{Fmt.Int(_engine.TodayLines)} ln today",
            MenuBarMetric.TodayMessages => $"{Fmt.Int(_engine.TodayMessages)} msg today",
            MenuBarMetric.TodaySessions => $"{_engine.TodaySessions} sess today",
            _ => "Pi Stats"
        };
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
        settings.Click += (_, _) => SettingsWindow.Open();

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
