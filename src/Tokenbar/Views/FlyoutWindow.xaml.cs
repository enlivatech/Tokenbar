using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Tokenbar.Interop;
using Tokenbar.Models;
using Tokenbar.Services;
using Windows.Graphics;
using Windows.UI;

namespace Tokenbar.Views;

public sealed partial class FlyoutWindow : Window
{
    private const double PanelWidth = 380;
    private const int TabColumns = 5;
    private const double ScreenMargin = 12;

    private readonly UsageStore _store;
    private readonly nint _hwnd;
    private DateTime _hiddenAt = DateTime.MinValue;
    private bool _showOverview = true;
    private readonly Native.WinEventProc _foregroundProc;
    private nint _foregroundHook;

    public event Action? QuitRequested;
    public event Action<SettingsPage>? SettingsRequested;

    public FlyoutWindow(UsageStore store)
    {
        _store = store;
        _foregroundProc = OnForegroundChanged;
        InitializeComponent();
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        SystemBackdrop = new DesktopAcrylicBackdrop();
        ExtendsContentIntoTitleBar = true;

        var presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;

        int corner = Native.DWMWCP_ROUND;
        Native.DwmSetWindowAttribute(_hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        RefreshText.Text = Strings.Refresh;
        SettingsText.Text = Strings.Settings;
        AboutText.Text = Strings.About;
        QuitText.Text = Strings.Quit;
        _store.Settings.Changed += () => { if (AppWindow.IsVisible) { Render(); ResizeToContent(); } };

        Activated += OnActivated;
        AppWindow.Closing += (_, e) => { e.Cancel = true; HideFlyout(); };
        _store.Changed += () =>
        {
            if (!AppWindow.IsVisible) return;
            Render();
            ResizeToContent();
        };
    }

    public bool IsOpen => AppWindow.IsVisible;

    /// <summary>Don't light-dismiss until the user first focuses the panel (dev <c>--open</c> runs can't take the foreground).</summary>
    public bool StayOpen { get; set; }

    internal void Toggle(Native.RECT? anchor)
    {
        if (AppWindow.IsVisible) { HideFlyout(); return; }
        // A click on the tray icon first deactivates (and hides) the flyout; don't reopen it.
        if ((DateTime.UtcNow - _hiddenAt).TotalMilliseconds < 300) return;
        ShowFlyout(anchor);
    }

    internal void ShowFlyout(Native.RECT? anchor)
    {
        Render();
        PositionNear(anchor);
        AppWindow.Show(true);
        Activate();
        Native.SetForegroundWindow(_hwnd);
        if (_foregroundHook == 0)
        {
            _foregroundHook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                0, _foregroundProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);
        }
    }

    public void HideFlyout()
    {
        if (_foregroundHook != 0)
        {
            Native.UnhookWinEvent(_foregroundHook);
            _foregroundHook = 0;
        }
        if (!AppWindow.IsVisible) return;
        _hiddenAt = DateTime.UtcNow;
        AppWindow.Hide();
    }

    // The flyout often never becomes the active window (focus-steal rules), so Deactivated alone
    // can't be trusted for light-dismiss; any other window coming to the foreground also closes it.
    private void OnForegroundChanged(nint hook, uint ev, nint hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (hwnd == _hwnd)
        {
            StayOpen = false;
            return;
        }
        if (!StayOpen) HideFlyout();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            if (!StayOpen) HideFlyout();
        }
        else if (args.WindowActivationState == WindowActivationState.PointerActivated)
        {
            StayOpen = false;
        }
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape) HideFlyout();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await _store.RefreshAllAsync();

    private void Quit_Click(object sender, RoutedEventArgs e) => QuitRequested?.Invoke();

    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(SettingsPage.General);

    private void About_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(SettingsPage.About);

    private double Scale => Native.GetDpiForWindow(_hwnd) / 96.0;

    private void PositionNear(Native.RECT? anchor)
    {
        Native.POINT pt;
        if (anchor is { } r) pt = new Native.POINT { X = (r.Left + r.Right) / 2, Y = (r.Top + r.Bottom) / 2 };
        else Native.GetCursorPos(out pt);

        var monitor = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
        var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.MONITORINFO>() };
        Native.GetMonitorInfoW(monitor, ref mi);
        Native.GetDpiForMonitor(monitor, 0, out uint dpi, out _);
        double scale = dpi / 96.0;
        var work = mi.rcWork;

        Root.Measure(new Windows.Foundation.Size(PanelWidth, double.PositiveInfinity));
        double maxHeight = (work.Bottom - work.Top) / scale - 2 * ScreenMargin;
        double heightDip = Math.Min(Root.DesiredSize.Height, maxHeight);

        int w = (int)Math.Round(PanelWidth * scale);
        int h = (int)Math.Round(heightDip * scale);
        int m = (int)Math.Round(ScreenMargin * scale);

        // Which edge the taskbar sits on decides whether the panel opens up, down, left or right.
        int x = Math.Clamp(pt.X - w / 2, work.Left + m, work.Right - w - m);
        int y = Math.Clamp(pt.Y - h / 2, work.Top + m, work.Bottom - h - m);
        if (mi.rcWork.Bottom < mi.rcMonitor.Bottom) y = work.Bottom - h - m;
        else if (mi.rcWork.Top > mi.rcMonitor.Top) y = work.Top + m;
        else if (mi.rcWork.Left > mi.rcMonitor.Left) x = work.Left + m;
        else if (mi.rcWork.Right < mi.rcMonitor.Right) x = work.Right - w - m;
        else y = work.Bottom - h - m;

        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }

    private void Render()
    {
        RenderTabs();
        RenderContent();
    }

    private void RenderTabs()
    {
        TabGrid.Children.Clear();
        TabGrid.ColumnDefinitions.Clear();
        TabGrid.RowDefinitions.Clear();
        var providers = _store.Enabled;
        // Overview only earns its place when there is more than one tool to compare.
        bool hasOverview = providers.Count > 1;
        if (!hasOverview) _showOverview = false;

        var tabs = new List<Button>();
        if (hasOverview)
        {
            tabs.Add(BuildTab(Strings.Overview, Strings.Overview, fg => new FontIcon { Glyph = "\uF0E2", FontSize = 16, Foreground = fg },
                _showOverview, null, null, () => _showOverview = true));
        }
        foreach (var id in providers)
        {
            var info = Providers.Get(id);
            var snapshot = _store.Get(id);
            // Mini meter under each tab shows remaining primary quota, like CodexBar's switcher.
            var remaining = snapshot?.Primary?.RemainingPercent ?? snapshot?.Secondary?.RemainingPercent;
            tabs.Add(BuildTab(info.TabName, info.DisplayName, fg => ProviderIconFactory.Create(info.IconName, 16, fg),
                !_showOverview && id == _store.Selected, remaining ?? -1, BrandBrush(info),
                () => { _showOverview = false; _store.Selected = id; }));
        }

        for (int c = 0; c < TabColumns; c++) TabGrid.ColumnDefinitions.Add(new ColumnDefinition());
        int rows = (tabs.Count + TabColumns - 1) / TabColumns;
        for (int r = 0; r < rows; r++) TabGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < tabs.Count; i++)
        {
            Grid.SetColumn(tabs[i], i % TabColumns);
            Grid.SetRow(tabs[i], i / TabColumns);
            TabGrid.Children.Add(tabs[i]);
        }
    }

    /// <param name="remaining">Meter value; null hides the meter, negative shows an empty track.</param>
    private Button BuildTab(string label, string tooltip, Func<Brush, FrameworkElement> icon, bool selected,
        double? remaining, Brush? meterBrush, Action select)
    {
        var fg = selected ? Brush("TextOnAccentFillColorPrimaryBrush") : Brush("TextFillColorPrimaryBrush");

        var panel = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Stretch };
        var glyph = icon(fg);
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(glyph);
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = fg,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        panel.Children.Add(remaining is double r
            ? MiniMeter(r < 0 ? null : r, selected ? fg : meterBrush ?? fg)
            : new Border { Height = 2, Margin = new Thickness(0, 2, 0, 0) });

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["ProviderTabButtonStyle"],
            Content = panel,
        };
        if (selected)
        {
            button.Background = Brush("AccentFillColorDefaultBrush");
            button.Resources["ButtonBackgroundPointerOver"] = Brush("AccentFillColorSecondaryBrush");
            button.Resources["ButtonBackgroundPressed"] = Brush("AccentFillColorTertiaryBrush");
        }
        ToolTipService.SetToolTip(button, tooltip);
        button.Click += (_, _) =>
        {
            select();
            Render();
            ResizeToContent();
        };
        return button;
    }

    private FrameworkElement MiniMeter(double? remaining, Brush fill)
    {
        var track = new Grid
        {
            Height = 2,
            Margin = new Thickness(8, 2, 8, 0),
            CornerRadius = new CornerRadius(1),
            Background = Brush("ControlStrongFillColorDisabledBrush"),
        };
        if (remaining is double r)
        {
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(r, 0.001), GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(100 - r, 0.001), GridUnitType.Star) });
            var bar = new Border { Background = fill, CornerRadius = new CornerRadius(1) };
            track.Children.Add(bar);
        }
        return track;
    }

    private void ResizeToContent()
    {
        var pos = AppWindow.Position;
        var size = AppWindow.Size;
        Root.Measure(new Windows.Foundation.Size(PanelWidth, double.PositiveInfinity));
        int bottom = pos.Y + size.Height;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        int maxH = area.Height - (int)Math.Round(2 * ScreenMargin * Scale);
        int h = Math.Min((int)Math.Round(Root.DesiredSize.Height * Scale), maxH);
        if (h == size.Height) return;
        if (bottom - h < area.Y) bottom = area.Y + h;
        // Keep the edge nearest the taskbar fixed (the bottom edge in the common layout).
        AppWindow.MoveAndResize(new RectInt32(pos.X, bottom - h, size.Width, h));
    }

    private void RenderContent()
    {
        ContentPanel.Children.Clear();
        if (_showOverview)
        {
            RenderOverview();
            return;
        }
        var info = Providers.Get(_store.Selected);
        var snapshot = _store.Get(_store.Selected);
        var brand = BrandBrush(info);

        // Header: name, updated time, plan.
        var header = new StackPanel { Spacing = 2 };
        var titleRow = new Grid();
        titleRow.Children.Add(new TextBlock { Text = info.DisplayName, Style = TextStyle("SubtitleTextBlockStyle") });
        if (_store.IsLoading(_store.Selected))
        {
            titleRow.Children.Add(new ProgressRing { Width = 16, Height = 16, IsActive = true, HorizontalAlignment = HorizontalAlignment.Right });
        }
        header.Children.Add(titleRow);
        header.Children.Add(TwoColumn(
            Secondary(snapshot is null ? Strings.Loading : Strings.Updated(snapshot.UpdatedAt)),
            Secondary(snapshot?.PlanName ?? snapshot?.AccountEmail ?? "")));
        ContentPanel.Children.Add(header);

        if (_store.UsingSampleData)
        {
            ContentPanel.Children.Add(new InfoBar { IsOpen = true, IsClosable = false, Severity = InfoBarSeverity.Informational, Message = Strings.SampleData });
        }

        if (snapshot?.Error is { } error)
        {
            ContentPanel.Children.Add(new InfoBar
            {
                IsOpen = true,
                IsClosable = false,
                Severity = InfoBarSeverity.Warning,
                Title = Strings.ErrorTitle,
                Message = error.Length > 300 ? error[..300] + "…" : error,
            });
        }

        if (snapshot is null) return;

        var quotas = snapshot.AllWindows.Where(w => !w.IsInformational).ToList();
        var infos = snapshot.AllWindows.Where(w => w.IsInformational).ToList();
        if (quotas.Count > 0) ContentPanel.Children.Add(Divider());
        foreach (var w in quotas)
        {
            ContentPanel.Children.Add(WindowSection(w, brand));
        }

        if (infos.Count > 0)
        {
            ContentPanel.Children.Add(Divider());
            var list = new StackPanel { Spacing = 4 };
            foreach (var w in infos)
            {
                var title = new TextBlock { Text = Strings.WindowTitle(w.Title), Style = TextStyle("BodyTextBlockStyle"), TextTrimming = TextTrimming.CharacterEllipsis };
                list.Children.Add(TwoColumn(title, Secondary(w.Detail ?? Strings.Used(w.UsedPercent))));
            }
            ContentPanel.Children.Add(list);
        }

        if (snapshot.Spend is { } spend)
        {
            ContentPanel.Children.Add(Divider());
            ContentPanel.Children.Add(SpendSection(spend, brand));
        }

        if (snapshot.Details is { Count: > 0 } details)
        {
            ContentPanel.Children.Add(Divider());
            var list = new StackPanel { Spacing = 4 };
            foreach (var d in details)
                list.Children.Add(TwoColumn(new TextBlock { Text = d.Title, Style = TextStyle("BodyTextBlockStyle") }, Secondary(d.Value)));
            ContentPanel.Children.Add(list);
        }
    }

    private void RenderOverview()
    {
        var enabled = _store.Enabled;
        var header = new Grid();
        header.Children.Add(new TextBlock { Text = Strings.Overview, Style = TextStyle("SubtitleTextBlockStyle") });
        if (_store.AnyLoading)
            header.Children.Add(new ProgressRing { Width = 16, Height = 16, IsActive = true, HorizontalAlignment = HorizontalAlignment.Right });
        ContentPanel.Children.Add(header);

        if (_store.UsingSampleData)
            ContentPanel.Children.Add(new InfoBar { IsOpen = true, IsClosable = false, Severity = InfoBarSeverity.Informational, Message = Strings.SampleData });

        var spends = enabled
            .Select(id => (Id: id, Spend: _store.Get(id)?.Spend))
            .Where(x => x.Spend is { IsApiValue: false })
            .Select(x => (x.Id, Spend: x.Spend!))
            .ToList();
        if (spends.Count > 0) ContentPanel.Children.Add(SpendSummaryCard(spends, enabled.Count));

        var cards = new StackPanel { Spacing = 6 };
        foreach (var id in enabled) cards.Children.Add(OverviewCard(id));
        ContentPanel.Children.Add(cards);
    }

    private FrameworkElement SpendSummaryCard(IReadOnlyList<(ProviderId Id, SpendSummary Spend)> spends, int providerCount)
    {
        var panel = new StackPanel { Spacing = 4 };
        SpendGroup(panel, Strings.SpendSummaryTitle, spends);
        panel.Children.Add(Secondary(Strings.SpendCoverage(spends.Count, providerCount)));
        return Card(panel);
    }

    /// <summary>One provider: "Title · Cursor", big amount, plan line. Several: title, then a row per provider.</summary>
    private static void SpendGroup(StackPanel panel, string title, IReadOnlyList<(ProviderId Id, SpendSummary Spend)> items)
    {
        static string Amount(SpendSummary s) => $"{s.CurrencySymbol} {s.Used:0.00}";
        static string? Plan(SpendSummary s) => s.Limit is double l ? Strings.PlanIncluded(s.CurrencySymbol, l) : null;

        if (items.Count == 1)
        {
            var (id, s) = items[0];
            panel.Children.Add(Secondary($"{title} · {Providers.Get(id).DisplayName}"));
            panel.Children.Add(new TextBlock { Text = Amount(s), Style = TextStyle("SubtitleTextBlockStyle") });
            if (Plan(s) is { } plan) panel.Children.Add(Secondary(plan));
            return;
        }

        panel.Children.Add(Secondary(title));
        foreach (var (id, s) in items)
        {
            var right = Plan(s) is { } plan ? $"{Amount(s)} / {plan}" : Amount(s);
            panel.Children.Add(TwoColumn(
                new TextBlock { Text = Providers.Get(id).DisplayName, Style = TextStyle("BodyStrongTextBlockStyle") },
                new TextBlock { Text = right, Style = TextStyle("BodyTextBlockStyle") }));
        }
    }

    private FrameworkElement OverviewCard(ProviderId id)
    {
        var info = Providers.Get(id);
        var snapshot = _store.Get(id);
        var brand = BrandBrush(info);
        var body = new StackPanel { Spacing = 6 };

        var titleRow = new Grid { ColumnSpacing = 8 };
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = ProviderIconFactory.Create(info.IconName, 14, Brush("TextFillColorPrimaryBrush"));
        icon.VerticalAlignment = VerticalAlignment.Center;
        titleRow.Children.Add(icon);
        var name = new TextBlock { Text = info.DisplayName, Style = TextStyle("BodyStrongTextBlockStyle") };
        Grid.SetColumn(name, 1);
        titleRow.Children.Add(name);
        string status = snapshot is null
            ? (_store.IsLoading(id) ? Strings.Loading : "")
            : snapshot.PlanName ?? "";
        var statusText = Secondary(status);
        statusText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(statusText, 2);
        titleRow.Children.Add(statusText);
        body.Children.Add(titleRow);

        var quotas = snapshot?.AllWindows.Where(w => !w.IsInformational).Take(3).ToList() ?? [];
        foreach (var w in quotas) body.Children.Add(CompactWindowRow(w, brand));

        if (snapshot?.Error is { } error && quotas.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = error,
                Style = TextStyle("CaptionTextBlockStyle"),
                Foreground = Brush("SystemFillColorCautionBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        else if (snapshot?.Spend is { } spend && quotas.Count == 0)
        {
            body.Children.Add(Secondary(spend.IsApiValue
                ? Strings.ApiValueLine(spend.CurrencySymbol, spend.Used, spend.Limit)
                : Strings.SpendLine(spend.Period, $"{spend.CurrencySymbol} {spend.Used:0.00}")));
        }

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["OverviewCardButtonStyle"],
            Content = body,
        };
        button.Click += (_, _) =>
        {
            _showOverview = false;
            _store.Selected = id;
            Render();
            ResizeToContent();
        };
        return button;
    }

    private static FrameworkElement CompactWindowRow(RateWindow w, Brush brand)
    {
        var row = new Grid { ColumnSpacing = 6 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

        var title = new TextBlock { Text = Strings.WindowTitle(w.Title), Style = TextStyle("CaptionTextBlockStyle"), TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var meter = Meter(w.UsedPercent, brand);
        meter.VerticalAlignment = VerticalAlignment.Center;
        var used = new TextBlock { Text = $"{w.UsedPercent:0}%", Style = TextStyle("CaptionTextBlockStyle"), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var reset = Secondary(w.ResetsAt is { } at ? Strings.ShortDuration(at - DateTimeOffset.Now) : "");
        reset.TextWrapping = TextWrapping.NoWrap;
        reset.HorizontalAlignment = HorizontalAlignment.Right;
        reset.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(meter, 1);
        Grid.SetColumn(used, 2);
        Grid.SetColumn(reset, 3);
        row.Children.Add(title);
        row.Children.Add(meter);
        row.Children.Add(used);
        row.Children.Add(reset);
        return row;
    }

    private static Border Card(UIElement child) => new()
    {
        Child = child,
        Padding = new Thickness(12, 10, 12, 10),
        CornerRadius = new CornerRadius(8),
        BorderThickness = new Thickness(1),
        Background = Brush("CardBackgroundFillColorDefaultBrush"),
        BorderBrush = Brush("CardStrokeColorDefaultBrush"),
    };

    private FrameworkElement WindowSection(RateWindow w, Brush brand)
    {
        var section = new StackPanel { Spacing = 6 };
        section.Children.Add(new TextBlock { Text = Strings.WindowTitle(w.Title), Style = TextStyle("BodyStrongTextBlockStyle") });
        section.Children.Add(Meter(w.UsedPercent, brand));

        string? reset = w.ResetsAt is { } at ? Strings.ResetsIn(at - DateTimeOffset.Now) : null;
        section.Children.Add(TwoColumn(
            new TextBlock { Text = Strings.Used(w.UsedPercent), Style = TextStyle("BodyTextBlockStyle") },
            Secondary(reset ?? "")));
        if (w.Detail is { Length: > 0 } detail) section.Children.Add(Secondary(detail));
        if (w.Pace is { } pace) section.Children.Add(Secondary(Strings.Pace(pace)));
        return section;
    }

    private FrameworkElement SpendSection(SpendSummary s, Brush brand)
    {
        var section = new StackPanel { Spacing = 6 };
        double? percent = s.Limit is > 0 ? s.Used / s.Limit * 100 : null;
        if (s.IsApiValue)
        {
            section.Children.Add(new TextBlock { Text = Strings.ApiValueTitle, Style = TextStyle("BodyStrongTextBlockStyle") });
            if (percent is double ap) section.Children.Add(Meter(ap, brand));
            section.Children.Add(TwoColumn(
                new TextBlock { Text = Strings.ApiValueLine(s.CurrencySymbol, s.Used, s.Limit), Style = TextStyle("BodyTextBlockStyle") },
                Secondary(percent is double app ? Strings.OfPlan(app) : "")));
            section.Children.Add(Secondary(Strings.ApiValueHint));
            return section;
        }

        section.Children.Add(new TextBlock { Text = Strings.Spend, Style = TextStyle("BodyStrongTextBlockStyle") });
        string amount = s.Limit is double limit
            ? $"{s.CurrencySymbol} {s.Used:0.00} / {s.CurrencySymbol} {limit:0.00}"
            : $"{s.CurrencySymbol} {s.Used:0.00}";
        if (percent is double p) section.Children.Add(Meter(p, brand));
        section.Children.Add(TwoColumn(
            new TextBlock { Text = Strings.SpendLine(s.Period, amount), Style = TextStyle("BodyTextBlockStyle") },
            Secondary(percent is double pp ? Strings.Used(pp) : "")));
        return section;
    }

    private const double MeterHeight = 6;

    private static Grid Meter(double percent, Brush brand)
    {
        double p = Math.Clamp(percent, 0, 100);
        var track = new Grid
        {
            Height = MeterHeight,
            CornerRadius = new CornerRadius(MeterHeight / 2),
            Background = Brush("ControlStrongFillColorDisabledBrush"),
        };
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(p, 0.001), GridUnitType.Star) });
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(100 - p, 0.001), GridUnitType.Star) });
        if (p > 0)
            track.Children.Add(new Border { Background = brand, CornerRadius = new CornerRadius(MeterHeight / 2) });
        return track;
    }

    private static Grid TwoColumn(FrameworkElement left, FrameworkElement right)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    private static TextBlock Secondary(string text) => new()
    {
        Text = text,
        Style = TextStyle("CaptionTextBlockStyle"),
        Foreground = Brush("TextFillColorSecondaryBrush"),
        TextWrapping = TextWrapping.Wrap,
    };

    private static Border Divider() => new() { Height = 1, Background = Brush("DividerStrokeColorDefaultBrush") };

    private static Style TextStyle(string key) => (Style)Application.Current.Resources[key];

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

    private static Brush BrandBrush(ProviderInfo info)
    {
        if (info.BrandColor is not { Length: 7 } hex) return Brush("TextFillColorPrimaryBrush");
        var c = Convert.ToUInt32(hex[1..], 16);
        return new SolidColorBrush(Color.FromArgb(255, (byte)(c >> 16), (byte)(c >> 8), (byte)c));
    }
}
