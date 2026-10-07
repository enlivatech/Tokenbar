using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Tokenbar.Interop;
using Tokenbar.Models;
using Tokenbar.Services;
using Tokenbar.Tray;
using Tokenbar.Views;

namespace Tokenbar;

public partial class App : Application
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private Mutex? _singleInstance;
    private TrayIcon? _tray;
    private FlyoutWindow? _flyout;
    private UsageStore? _store;
    private DispatcherQueueTimer? _timer;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var argv = Environment.GetCommandLineArgs();
        int dump = Array.IndexOf(argv, "--dump-icons");
        if (dump >= 0 && dump + 1 < argv.Length)
        {
            Directory.CreateDirectory(argv[dump + 1]);
            foreach (int size in new[] { 16, 20, 24, 32 })
            {
                TrayIconRenderer.DumpAlpha(Path.Combine(argv[dump + 1], $"icon-{size}-full.raw"), size, 100, 100, false);
                TrayIconRenderer.DumpAlpha(Path.Combine(argv[dump + 1], $"icon-{size}-mixed.raw"), size, 63, 84, false);
                TrayIconRenderer.DumpAlpha(Path.Combine(argv[dump + 1], $"icon-{size}-low.raw"), size, 0, 12, false);
                TrayIconRenderer.DumpAlpha(Path.Combine(argv[dump + 1], $"icon-{size}-stale.raw"), size, 63, 84, true);
            }
            Exit();
            return;
        }

        _singleInstance = new Mutex(true, @"Local\Tokenbar.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Exit();
            return;
        }

        var cli = CliLocator.Find();
        IEnumerable<IUsageFetcher> fetchers = cli is null
            ? Providers.All.Select(p => (IUsageFetcher)new SampleUsageFetcher(p.Id))
            : Providers.All.Select(p => (IUsageFetcher)new CliUsageFetcher(p.Id, cli));
        _store = new UsageStore(fetchers, usingSampleData: cli is null);

        _flyout = new FlyoutWindow(_store);
        _flyout.QuitRequested += Quit;

        _tray = new TrayIcon();
        _tray.LeftClicked += () => _flyout.Toggle(_tray.GetIconRect());
        _tray.RightClicked += () => _flyout.Toggle(_tray.GetIconRect());
        _tray.DisplaySettingsChanged += UpdateTrayIcon;
        _store.Changed += UpdateTrayIcon;
        UpdateTrayIcon();

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = RefreshInterval;
        _timer.Tick += async (_, _) => await _store.RefreshAllAsync();
        _timer.Start();

        if (argv.Contains("--open"))
        {
            _flyout.StayOpen = true;
            _flyout.ShowFlyout(_tray.GetIconRect());
        }

        await _store.RefreshAllAsync();
    }

    private void UpdateTrayIcon()
    {
        if (_tray is null || _store is null) return;
        var info = Providers.Get(_store.Selected);
        var snapshot = _store.Get(_store.Selected);
        var primary = snapshot?.Primary?.RemainingPercent;
        var secondary = snapshot?.Secondary?.RemainingPercent;
        bool stale = snapshot is null || snapshot.Error is not null;

        var hIcon = TrayIconRenderer.CreateIcon(TrayIconSize(), primary, secondary, stale, IsTaskbarLight());
        _tray.Update(hIcon, Tooltip(info, snapshot));
    }

    private static string Tooltip(ProviderInfo info, UsageSnapshot? snapshot)
    {
        if (snapshot is null) return $"Tokenbar · {info.DisplayName}";
        var parts = snapshot.AllWindows.Take(2)
            .Select(w => $"{Strings.WindowTitle(w.Title)} {Strings.Left(w.RemainingPercent)}");
        return $"{info.DisplayName} · {string.Join(" · ", parts)}";
    }

    private int TrayIconSize()
    {
        uint dpi = Native.GetDpiForSystem();
        if (_tray?.GetIconRect() is { } r)
        {
            var monitor = Native.MonitorFromPoint(new Native.POINT { X = r.Left, Y = r.Top }, Native.MONITOR_DEFAULTTONEAREST);
            if (Native.GetDpiForMonitor(monitor, 0, out uint mdpi, out _) == 0) dpi = mdpi;
        }
        return Native.GetSystemMetricsForDpi(Native.SM_CXSMICON, dpi);
    }

    private static bool IsTaskbarLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is int v && v == 1;
    }

    private void Quit()
    {
        _timer?.Stop();
        _tray?.Dispose();
        _singleInstance?.ReleaseMutex();
        Exit();
    }
}
