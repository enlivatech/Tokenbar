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

    public static string Loading => Zh ? "正在读取用量…" : "Loading usage…";
    public static string Refresh => Zh ? "刷新" : "Refresh";
    public static string Settings => Zh ? "设置…" : "Settings…";
    public static string About => Zh ? "关于 Tokenbar" : "About Tokenbar";
    public static string Quit => Zh ? "退出" : "Quit";
    public static string SampleData => Zh ? "示例数据：没有找到 codexbar CLI" : "Sample data: codexbar CLI not found";
    public static string ErrorTitle => Zh ? "读取失败" : "Couldn't load usage";
}
