using System.Globalization;

namespace Tokenbar.Services;

internal static class Strings
{
    private static readonly bool Zh = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh";

    public static string Used(double percent) => Zh ? $"已用 {percent:0}%" : $"{percent:0}% used";
    public static string Left(double percent) => Zh ? $"剩余 {percent:0}%" : $"{percent:0}% left";

    public static string ResetsIn(TimeSpan span) => Zh ? $"{Duration(span)}后重置" : $"Resets in {Duration(span)}";

    public static string Duration(TimeSpan span)
    {
        if (span < TimeSpan.FromMinutes(1)) return Zh ? "不到 1 分钟" : "<1m";
        if (span.TotalHours < 1) return Zh ? $"{span.Minutes} 分钟" : $"{span.Minutes}m";
        if (span.TotalDays < 1) return Zh ? $"{(int)span.TotalHours} 小时 {span.Minutes} 分" : $"{(int)span.TotalHours}h {span.Minutes}m";
        return Zh ? $"{(int)span.TotalDays} 天 {span.Hours} 小时" : $"{(int)span.TotalDays}d {span.Hours}h";
    }

    public static string ShortDuration(TimeSpan span)
    {
        if (span < TimeSpan.FromMinutes(1)) return "<1m";
        if (span.TotalHours < 1) return Zh ? $"{span.Minutes}分" : $"{span.Minutes}m";
        if (span.TotalDays < 1) return Zh ? $"{(int)span.TotalHours}时{span.Minutes}分" : $"{(int)span.TotalHours}h {span.Minutes}m";
        return Zh ? $"{(int)span.TotalDays}天{span.Hours}时" : $"{(int)span.TotalDays}d {span.Hours}h";
    }

    public static string Updated(DateTimeOffset at)
    {
        var ago = DateTimeOffset.Now - at;
        if (ago < TimeSpan.FromMinutes(1)) return Zh ? "刚刚更新" : "Updated just now";
        return Zh ? $"{Duration(ago)}前更新" : $"Updated {Duration(ago)} ago";
    }

    public static string WindowTitle(string title) => !Zh ? title : title switch
    {
        "Session" => "会话",
        "Weekly" => "每周",
        "Daily" => "每日",
        "Monthly" => "每月",
        "Model" => "模型",
        "Total" => "总计",
        "Third Party" => "第三方模型",
        "Credits" => "额度",
        _ => title,
    };

    public static string Pace(Models.UsagePace pace)
    {
        var d = pace.DeltaPercent;
        string state = Math.Abs(d) < 5
            ? (Zh ? "正常" : "On track")
            : d > 0 ? (Zh ? $"偏快（+{d:0}%）" : $"Ahead (+{d:0}%)") : (Zh ? $"偏慢（{d:0}%）" : $"Behind ({d:0}%)");
        string lasts = pace.WillLastToReset
            ? (Zh ? "能撑到重置" : "Lasts to reset")
            : (Zh ? "重置前会用完" : "Runs out before reset");
        return Zh ? $"节奏：{state} · {lasts}" : $"Pace: {state} · {lasts}";
    }

    public static string Spend => Zh ? "额外用量" : "Extra usage";
    public static string SpendLine(string period, string amount) => Zh ? $"{PeriodName(period)}：{amount}" : $"{PeriodName(period)}: {amount}";

    private static string PeriodName(string period) => period.ToLowerInvariant() switch
    {
        "monthly" or "month" => Zh ? "本月" : "This month",
        "daily" or "day" => Zh ? "今天" : "Today",
        "weekly" or "week" => Zh ? "本周" : "This week",
        "" => Zh ? "本期" : "This period",
        _ => period,
    };

    public static string Overview => Zh ? "总览" : "Overview";
    public static string SpendSummaryTitle => Zh ? "本期花费" : "Usage & Spend";
    public static string ApiValueTitle => Zh ? "API 价值" : "API value";
    public static string ApiValueHint => Zh ? "按 API 价格折算的用量，不是实际扣费" : "Usage priced at API rates, not what you're billed";
    public static string ApiValueLine(string symbol, double used, double? plan) => plan is double p
        ? (Zh ? $"API 价值 {symbol}{used:0.00} / 套餐 {symbol}{p:0.00}" : $"{symbol}{used:0.00} API value / {symbol}{p:0.00} plan")
        : (Zh ? $"API 价值 {symbol}{used:0.00}" : $"{symbol}{used:0.00} API value");
    public static string PlanIncluded(string symbol, double plan) => Zh ? $"套餐 {symbol}{plan:0.00}" : $"{symbol}{plan:0.00} plan";
    public static string OfPlan(double percent) => Zh ? $"套餐的 {percent:0}%" : $"{percent:0}% of plan";
    public static string SpendCoverage(int withSpend, int total) =>
        Zh ? $"{total} 个工具中有 {withSpend} 个有花费" : $"{withSpend} of {total} tools have spend";

    public static string Loading => Zh ? "正在读取用量…" : "Loading usage…";
    public static string Refresh => Zh ? "刷新" : "Refresh";
    public static string Settings => Zh ? "设置…" : "Settings…";
    public static string About => Zh ? "关于 Tokenbar" : "About Tokenbar";
    public static string Quit => Zh ? "退出" : "Quit";
    public static string SampleData => Zh ? "示例数据：没有找到 codexbar CLI" : "Sample data: codexbar CLI not found";
    public static string ErrorTitle => Zh ? "读取失败" : "Couldn't load usage";

    public static string SettingsTitle => Zh ? "Tokenbar 设置" : "Tokenbar Settings";
    public static string PageGeneral => Zh ? "通用" : "General";
    public static string PageProviders => Zh ? "工具" : "Providers";
    public static string PageAbout => Zh ? "关于" : "About";

    public static string LaunchAtLogin => Zh ? "开机时启动" : "Launch at login";
    public static string LaunchAtLoginHint => Zh ? "登录 Windows 后自动出现在托盘" : "Start in the tray when you sign in to Windows";
    public static string RefreshInterval => Zh ? "刷新间隔" : "Refresh interval";
    public static string RefreshIntervalHint => Zh ? "多久自动读取一次各工具的用量" : "How often usage is fetched automatically";
    public static string Minutes(int m) => Zh ? $"{m} 分钟" : m == 1 ? "1 minute" : $"{m} minutes";
    public static string RefreshNow => Zh ? "立即刷新" : "Refresh now";
    public static string RefreshNowHint => Zh ? "重新读取所有已启用工具的用量" : "Fetch usage for all enabled providers";

    public static string ProvidersHint => Zh
        ? "打开开关就会在面板和托盘里显示。展开可以看数据来源，或填写 API Key / Cookie。密钥只交给 codexbar CLI，并用 Windows DPAPI 加密保存。"
        : "Enabled providers appear in the panel and tray. Expand one to see where its data comes from or to add an API key / cookie. Secrets are handed to the codexbar CLI and stored encrypted with Windows DPAPI.";
    public static string StatusDisabled => Zh ? "已关闭" : "Off";
    public static string StatusNotLoaded => Zh ? "尚未读取" : "Not loaded yet";
    public static string StatusConnected => Zh ? "已连接" : "Connected";

    public static string ProviderSourceHint(Models.ProviderId id) => id switch
    {
        Models.ProviderId.Codex => Zh ? "读取 Codex CLI 的登录（~/.codex）。没登录的话先在终端运行 codex login。" : "Uses your Codex CLI sign-in (~/.codex). Run `codex login` first if needed.",
        Models.ProviderId.Claude => Zh ? "读取 Claude Code 的登录凭据。需要允许下面的开关。" : "Uses your Claude Code credentials. Allow access below.",
        Models.ProviderId.Cursor => Zh ? "自动读取 Cursor 桌面端的登录；读不到时可以粘贴 cursor.com 的 Cookie。" : "Reads the Cursor desktop sign-in automatically; paste a cursor.com Cookie header if that fails.",
        Models.ProviderId.Gemini => Zh ? "需要安装并登录 Gemini CLI。Google 已停用个人账号的 Gemini CLI 登录，个人账号建议改用 Antigravity。" : "Requires a signed-in Gemini CLI. Google disabled Gemini CLI sign-in for personal accounts; use Antigravity instead.",
        Models.ProviderId.Antigravity => Zh ? "Antigravity 打开时自动读取本地额度。" : "Reads local quota while Antigravity is running.",
        Models.ProviderId.OpenCode => Zh ? "需要 opencode.ai 的 Cookie（浏览器开发者工具里复制请求头的 Cookie）。" : "Needs an opencode.ai Cookie header (copy it from your browser's DevTools).",
        Models.ProviderId.OpenCodeGo => Zh ? "默认读取本地记录估算；填写 opencode.ai 的 Cookie 可以拿到准确额度。" : "Estimates from local history; add an opencode.ai Cookie header for exact quota.",
        Models.ProviderId.Grok => Zh ? "需要 grok.com 的 Cookie（浏览器开发者工具里复制请求头的 Cookie）。" : "Needs a grok.com Cookie header (copy it from your browser's DevTools).",
        Models.ProviderId.OpenRouter => Zh ? "填写 OpenRouter 的 API Key（openrouter.ai/settings/keys）。" : "Add an OpenRouter API key (openrouter.ai/settings/keys).",
        _ => "",
    };

    public static string ClaudeCredentialsAllow => Zh ? "允许读取 Claude Code 凭据" : "Allow Claude Code credentials";
    public static string ApiKey => "API Key";
    public static string CookieHeader => "Cookie";
    public static string SecretStored => Zh ? "已保存（输入新值可替换）" : "Saved (type to replace)";
    public static string SecretPlaceholder => Zh ? "粘贴到这里" : "Paste here";
    public static string Save => Zh ? "保存" : "Save";
    public static string Remove => Zh ? "移除" : "Remove";
    public static string Saved => Zh ? "已保存，正在刷新" : "Saved, refreshing";

    public static string Version => Zh ? "版本" : "Version";
    public static string AboutTagline => Zh ? "在 Windows 托盘里看 AI 编程工具的用量" : "AI coding tool usage in your Windows tray";
    public static string Credits => Zh ? "致谢" : "Credits";
    public static string CreditCodexBar => Zh ? "面板与图标设计参考（MIT）" : "Panel and icon design (MIT)";
    public static string CreditWinCodexBar => Zh ? "用量读取 CLI 与工具图标（MIT）" : "Usage CLI and provider icons (MIT)";
}
