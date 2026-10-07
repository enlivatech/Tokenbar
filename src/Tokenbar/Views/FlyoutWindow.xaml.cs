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

    public event Action? QuitRequested;

    public FlyoutWindow(UsageStore store)
    {
        _store = store;
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
        QuitText.Text = Strings.Quit;

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

    /// <summary>Don't light-dismiss on focus loss (dev <c>--open</c> runs can't take the foreground).</summary>
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
    }

    public void HideFlyout()
    {
        if (!AppWindow.IsVisible) return;
        _hiddenAt = DateTime.UtcNow;
        AppWindow.Hide();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated && !StayOpen) HideFlyout();
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape) HideFlyout();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await _store.RefreshAllAsync();

    private void Quit_Click(object sender, RoutedEventArgs e) => QuitRequested?.Invoke();

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
        for (int c = 0; c < TabColumns; c++) TabGrid.ColumnDefinitions.Add(new ColumnDefinition());
        int rows = (providers.Count + TabColumns - 1) / TabColumns;
        for (int r = 0; r < rows; r++) TabGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < providers.Count; i++)
        {
            var tab = BuildTab(providers[i]);
            Grid.SetColumn(tab, i % TabColumns);
            Grid.SetRow(tab, i / TabColumns);
            TabGrid.Children.Add(tab);
        }
    }

    private Button BuildTab(ProviderId id)
    {
        var info = Providers.Get(id);
        bool selected = id == _store.Selected;
        var fg = selected ? Brush("TextOnAccentFillColorPrimaryBrush") : Brush("TextFillColorPrimaryBrush");

        var panel = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Stretch };
        var icon = ProviderIconFactory.Create(info.IconName, 16, fg);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(icon);
        panel.Children.Add(new TextBlock
        {
            Text = info.TabName,
            FontSize = 11,
            Foreground = fg,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        // Mini meter under each tab shows remaining primary quota, like CodexBar's switcher.
        var snapshot = _store.Get(id);
        var remaining = snapshot?.Primary?.RemainingPercent ?? snapshot?.Secondary?.RemainingPercent;
        panel.Children.Add(MiniMeter(remaining, selected ? fg : BrandBrush(info)));

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
        ToolTipService.SetToolTip(button, info.DisplayName);
        button.Click += (_, _) =>
        {
            _store.Selected = id;
            Render();
            ResizeToContent();
        };
        return button;
    }

    private FrameworkElement MiniMeter(double? remaining, Brush fill)
    {
        var track = new Grid
        {
            Height = 3,
            Margin = new Thickness(8, 1, 8, 0),
            CornerRadius = new CornerRadius(1.5),
            Background = Brush("ControlStrongFillColorDisabledBrush"),
        };
        if (remaining is double r)
        {
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(r, 0.001), GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(100 - r, 0.001), GridUnitType.Star) });
            var bar = new Border { Background = fill, CornerRadius = new CornerRadius(1.5) };
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
        section.Children.Add(new TextBlock { Text = Strings.Spend, Style = TextStyle("BodyStrongTextBlockStyle") });
        string amount = s.Limit is double limit
            ? $"{s.CurrencySymbol} {s.Used:0.00} / {s.CurrencySymbol} {limit:0.00}"
            : $"{s.CurrencySymbol} {s.Used:0.00}";
        double? percent = s.Limit is > 0 ? s.Used / s.Limit * 100 : null;
        if (percent is double p) section.Children.Add(Meter(p, brand));
        section.Children.Add(TwoColumn(
            new TextBlock { Text = Strings.SpendLine(s.Period, amount), Style = TextStyle("BodyTextBlockStyle") },
            Secondary(percent is double pp ? Strings.Used(pp) : "")));
        return section;
    }

    private static ProgressBar Meter(double percent, Brush brand) => new()
    {
        Minimum = 0,
        Maximum = 100,
        Value = Math.Clamp(percent, 0, 100),
        Foreground = brand,
        MinHeight = 4,
    };

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
