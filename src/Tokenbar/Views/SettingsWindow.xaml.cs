using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Tokenbar.Interop;
using Tokenbar.Models;
using Tokenbar.Services;
using Windows.Graphics;

namespace Tokenbar.Views;

public enum SettingsPage { General, Providers, About }

public sealed partial class SettingsWindow : Window
{
    private static readonly int[] RefreshChoices = [1, 2, 5, 15, 30];

    /// <summary>Providers whose CLI route can use a browser Cookie header.</summary>
    private static readonly HashSet<ProviderId> CookieProviders =
        [ProviderId.Cursor, ProviderId.OpenCode, ProviderId.OpenCodeGo, ProviderId.Grok];

    private readonly UsageStore _store;
    private readonly nint _hwnd;
    private Dictionary<string, CredentialStatus>? _credentials;
    private SettingsPage _page;

    public SettingsWindow(UsageStore store)
    {
        _store = store;
        InitializeComponent();
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        Title = Strings.SettingsTitle;
        TitleText.Text = Strings.SettingsTitle;
        GeneralItem.Content = Strings.PageGeneral;
        ProvidersItem.Content = Strings.PageProviders;
        AboutItem.Content = Strings.PageAbout;

        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarArea);

        double scale = Native.GetDpiForWindow(_hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(900 * scale), (int)(680 * scale)));
        if (AppWindow.Presenter is OverlappedPresenter p) p.IsMinimizable = true;

        _store.Changed += () => { if (_page == SettingsPage.Providers) RenderPage(); };
    }

    public void Open(SettingsPage page)
    {
        _page = page;
        Nav.SelectedItem = page switch
        {
            SettingsPage.Providers => ProvidersItem,
            SettingsPage.About => AboutItem,
            _ => GeneralItem,
        };
        RenderPage();
        Activate();
        Native.SetForegroundWindow(_hwnd);
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag }) return;
        _page = Enum.Parse<SettingsPage>(tag);
        RenderPage();
    }

    private void RenderPage()
    {
        PagePanel.Children.Clear();
        PagePanel.Children.Add(new TextBlock
        {
            Text = _page switch
            {
                SettingsPage.Providers => Strings.PageProviders,
                SettingsPage.About => Strings.PageAbout,
                _ => Strings.PageGeneral,
            },
            Style = TextStyle("TitleTextBlockStyle"),
            Margin = new Thickness(0, 0, 0, 16),
        });

        switch (_page)
        {
            case SettingsPage.General: RenderGeneral(); break;
            case SettingsPage.Providers: RenderProviders(); break;
            case SettingsPage.About: RenderAbout(); break;
        }
    }

    // ── General ──────────────────────────────────────────────────────────

    private void RenderGeneral()
    {
        var launch = new ToggleSwitch { IsOn = LaunchAtLogin.IsEnabled, OnContent = "", OffContent = "", MinWidth = 0 };
        launch.Toggled += (_, _) => LaunchAtLogin.Set(launch.IsOn);
        PagePanel.Children.Add(SettingsCard("\uE7E8", Strings.LaunchAtLogin, Strings.LaunchAtLoginHint, launch));

        var interval = new ComboBox { MinWidth = 140 };
        foreach (var m in RefreshChoices) interval.Items.Add(new ComboBoxItem { Content = Strings.Minutes(m), Tag = m });
        interval.SelectedIndex = Math.Max(0, Array.IndexOf(RefreshChoices, _store.Settings.RefreshMinutes));
        interval.SelectionChanged += (_, _) =>
        {
            if (interval.SelectedItem is ComboBoxItem { Tag: int m })
            {
                _store.Settings.RefreshMinutes = m;
                _store.Settings.Save();
            }
        };
        PagePanel.Children.Add(SettingsCard("\uE72C", Strings.RefreshInterval, Strings.RefreshIntervalHint, interval));

        var refresh = new Button { Content = Strings.RefreshNow };
        refresh.Click += async (_, _) =>
        {
            refresh.IsEnabled = false;
            await _store.RefreshAllAsync();
            refresh.IsEnabled = true;
        };
        PagePanel.Children.Add(SettingsCard("\uE895", Strings.Refresh, Strings.RefreshNowHint, refresh));
    }

    // ── Providers ────────────────────────────────────────────────────────

    private void RenderProviders()
    {
        if (CliRunner.Path is null)
        {
            PagePanel.Children.Add(new InfoBar { IsOpen = true, IsClosable = false, Severity = InfoBarSeverity.Warning, Message = Strings.SampleData });
        }
        else if (_credentials is null)
        {
            _ = LoadCredentialsAsync();
        }

        PagePanel.Children.Add(new TextBlock { Text = Strings.ProvidersHint, Style = TextStyle("BodyTextBlockStyle"), Foreground = Brush("TextFillColorSecondaryBrush"), Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });
        foreach (var info in Providers.All) PagePanel.Children.Add(ProviderExpander(info));
    }

    private async Task LoadCredentialsAsync()
    {
        try
        {
            _credentials = await CliConfig.GetCredentialsAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException or TimeoutException)
        {
            _credentials = new();
        }
        if (_page == SettingsPage.Providers) RenderPage();
    }

    private FrameworkElement ProviderExpander(ProviderInfo info)
    {
        var toggle = new ToggleSwitch { IsOn = _store.Settings.IsEnabled(info.Id), OnContent = "", OffContent = "", MinWidth = 0 };
        toggle.Toggled += (_, _) => _store.Settings.SetEnabled(info.Id, toggle.IsOn);

        var header = new Grid { ColumnSpacing = 16, MinHeight = 48, Padding = new Thickness(0, 8, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = ProviderIconFactory.Create(info.IconName, 20, Brush("TextFillColorPrimaryBrush"));
        icon.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(icon);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = info.DisplayName, Style = TextStyle("BodyTextBlockStyle") });
        var (status, isError) = StatusLine(info.Id);
        text.Children.Add(new TextBlock
        {
            Text = status,
            Style = TextStyle("CaptionTextBlockStyle"),
            Foreground = Brush(isError ? "SystemFillColorCautionBrush" : "TextFillColorSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        Grid.SetColumn(text, 1);
        header.Children.Add(text);

        toggle.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(toggle, 2);
        header.Children.Add(toggle);

        return new Expander
        {
            Header = header,
            Content = ProviderDetails(info),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            IsExpanded = false,
        };
    }

    private (string Text, bool IsError) StatusLine(ProviderId id)
    {
        if (!_store.Settings.IsEnabled(id)) return (Strings.StatusDisabled, false);
        if (_store.IsLoading(id) && _store.Get(id) is null) return (Strings.Loading, false);
        var snapshot = _store.Get(id);
        if (snapshot is null) return (Strings.StatusNotLoaded, false);
        if (snapshot.Error is { } error && !snapshot.AllWindows.Any())
            return (error.Split('\n')[0], true);
        var plan = snapshot.PlanName ?? snapshot.AccountEmail;
        return (plan is null ? Strings.StatusConnected : $"{Strings.StatusConnected} · {plan}", false);
    }

    private FrameworkElement ProviderDetails(ProviderInfo info)
    {
        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(36, 4, 0, 4) };
        panel.Children.Add(new TextBlock { Text = Strings.ProviderSourceHint(info.Id), Style = TextStyle("BodyTextBlockStyle"), TextWrapping = TextWrapping.Wrap, Foreground = Brush("TextFillColorSecondaryBrush") });

        if (info.Id == ProviderId.Claude && CliRunner.Path is not null)
        {
            var allow = new ToggleSwitch { OnContent = Strings.ClaudeCredentialsAllow, OffContent = Strings.ClaudeCredentialsAllow, IsEnabled = false };
            panel.Children.Add(allow);
            _ = InitClaudeToggleAsync(allow, info.Id);
        }

        var creds = _credentials?.GetValueOrDefault(info.CliName);
        if (creds?.AcceptsApiKey == true)
        {
            panel.Children.Add(SecretRow(info, Strings.ApiKey, creds.HasApiKey,
                v => CliConfig.SetApiKeyAsync(info.CliName, v), () => CliConfig.RemoveApiKeyAsync(info.CliName)));
        }
        if (CookieProviders.Contains(info.Id) && CliRunner.Path is not null)
        {
            panel.Children.Add(SecretRow(info, Strings.CookieHeader, creds?.HasCookie == true,
                v => CliConfig.SetCookieAsync(info.CliName, v), () => CliConfig.RemoveCookieAsync(info.CliName)));
        }
        return panel;
    }

    private async Task InitClaudeToggleAsync(ToggleSwitch toggle, ProviderId id)
    {
        try
        {
            toggle.IsOn = await CliConfig.GetClaudeCredentialsAllowedAsync();
            toggle.IsEnabled = true;
            toggle.Toggled += async (_, _) =>
            {
                await CliConfig.SetClaudeCredentialsAllowedAsync(toggle.IsOn);
                await _store.RefreshAsync(id);
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException or KeyNotFoundException or TimeoutException)
        {
            toggle.IsEnabled = false;
        }
    }

    private FrameworkElement SecretRow(ProviderInfo info, string label, bool stored,
        Func<string, Task<CliResult>> save, Func<Task<CliResult>> remove)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = label, Style = TextStyle("BodyStrongTextBlockStyle") });

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var box = new PasswordBox { PlaceholderText = stored ? Strings.SecretStored : Strings.SecretPlaceholder, MaxWidth = 520, HorizontalAlignment = HorizontalAlignment.Stretch };
        var saveButton = new Button { Content = Strings.Save, Style = TextStyle("AccentButtonStyle"), IsEnabled = false };
        var removeButton = new Button { Content = Strings.Remove, Visibility = stored ? Visibility.Visible : Visibility.Collapsed };
        box.PasswordChanged += (_, _) => saveButton.IsEnabled = box.Password.Trim().Length > 0;

        var result = new InfoBar { IsClosable = true, IsOpen = false };

        async Task Apply(Task<CliResult> action)
        {
            saveButton.IsEnabled = removeButton.IsEnabled = false;
            CliResult r;
            try { r = await action; }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException) { r = new CliResult(1, "", ex.Message); }
            result.Severity = r.Ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            result.Message = r.Ok ? Strings.Saved : r.Message;
            result.IsOpen = true;
            removeButton.IsEnabled = true;
            if (r.Ok)
            {
                box.Password = "";
                _credentials = null;
                _store.Settings.SetEnabled(info.Id, true);
                await _store.RefreshAsync(info.Id);
            }
        }

        saveButton.Click += async (_, _) => await Apply(save(box.Password.Trim()));
        removeButton.Click += async (_, _) => await Apply(remove());

        Grid.SetColumn(saveButton, 1);
        Grid.SetColumn(removeButton, 2);
        row.Children.Add(box);
        row.Children.Add(saveButton);
        row.Children.Add(removeButton);
        panel.Children.Add(row);
        panel.Children.Add(result);
        return panel;
    }

    // ── About ────────────────────────────────────────────────────────────

    private void RenderAbout()
    {
        var version = typeof(SettingsWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var head = new StackPanel { Spacing = 2 };
        head.Children.Add(new TextBlock { Text = "Tokenbar", Style = TextStyle("SubtitleTextBlockStyle") });
        head.Children.Add(new TextBlock { Text = Strings.AboutTagline, Style = TextStyle("BodyTextBlockStyle"), Foreground = Brush("TextFillColorSecondaryBrush") });
        PagePanel.Children.Add(SettingsCard("\uE946", $"{Strings.Version} {version}", CliRunner.Path ?? Strings.SampleData, null, head));

        PagePanel.Children.Add(new TextBlock { Text = Strings.Credits, Style = TextStyle("BodyStrongTextBlockStyle"), Margin = new Thickness(0, 16, 0, 4) });
        PagePanel.Children.Add(LinkCard("CodexBar", Strings.CreditCodexBar, "https://github.com/steipete/CodexBar"));
        PagePanel.Children.Add(LinkCard("Win-CodexBar", Strings.CreditWinCodexBar, "https://github.com/nesszer/Win-CodexBar"));
    }

    private FrameworkElement LinkCard(string title, string description, string url)
    {
        var link = new HyperlinkButton { Content = url, NavigateUri = new Uri(url) };
        return SettingsCard("\uE71B", title, description, link);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Win11 Settings-style card: glyph, title + description, trailing control.</summary>
    private static FrameworkElement SettingsCard(string glyph, string title, string? description, FrameworkElement? control, FrameworkElement? customText = null)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new FontIcon { Glyph = glyph, FontSize = 20, VerticalAlignment = VerticalAlignment.Center });

        FrameworkElement text = customText ?? BuildCardText(title, description);
        if (customText is not null && description is not null)
        {
            var stack = new StackPanel();
            stack.Children.Add(customText);
            stack.Children.Add(new TextBlock { Text = $"{title} · {description}", Style = TextStyle("CaptionTextBlockStyle"), Foreground = Brush("TextFillColorSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
            text = stack;
        }
        text.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        if (control is not null)
        {
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 2);
            grid.Children.Add(control);
        }

        return new Border
        {
            Child = grid,
            MinHeight = 68,
            Padding = new Thickness(16, 12, 16, 12),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            Background = Brush("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = Brush("CardStrokeColorDefaultBrush"),
        };
    }

    private static FrameworkElement BuildCardText(string title, string? description)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, Style = TextStyle("BodyTextBlockStyle") });
        if (description is not null)
            stack.Children.Add(new TextBlock { Text = description, Style = TextStyle("CaptionTextBlockStyle"), Foreground = Brush("TextFillColorSecondaryBrush"), TextWrapping = TextWrapping.Wrap });
        return stack;
    }

    private static Style TextStyle(string key) => (Style)Application.Current.Resources[key];

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
}
