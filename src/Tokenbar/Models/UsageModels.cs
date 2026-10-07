namespace Tokenbar.Models;

public enum ProviderId
{
    Codex,
    Claude,
    Cursor,
    Gemini,
    Antigravity,
    OpenCode,
    OpenCodeGo,
    Grok,
    OpenRouter,
}

/// <summary>A quota window such as "Session" (5h) or "Weekly".</summary>
public sealed record RateWindow(
    string Title,
    double UsedPercent,
    DateTimeOffset? ResetsAt = null,
    string? Detail = null,
    bool IsInformational = false,
    UsagePace? Pace = null)
{
    public double RemainingPercent => Math.Clamp(100 - UsedPercent, 0, 100);
}

/// <summary>How actual usage compares with an even burn over the window (CodexBar "Pace").</summary>
/// <param name="DeltaPercent">Used minus expected; negative means behind (good).</param>
public sealed record UsagePace(double DeltaPercent, bool WillLastToReset);

public sealed record CostSummary(
    decimal TodayCost,
    long TodayTokens,
    decimal Last30DaysCost,
    long Last30DaysTokens);

/// <summary>Spend against a limit for the period, e.g. "This month: $ 0.00 / $ 2000.00".</summary>
public sealed record SpendSummary(
    double Used,
    double? Limit,
    string CurrencySymbol,
    string Period,
    DateTimeOffset? ResetsAt,
    bool IsApiValue = false);

public sealed record DetailLine(string Title, string Value);

public sealed record UsageSnapshot(
    ProviderId Provider,
    DateTimeOffset UpdatedAt,
    string? PlanName,
    RateWindow? Primary,
    RateWindow? Secondary,
    IReadOnlyList<RateWindow> Extra,
    CostSummary? Cost = null,
    string? Error = null,
    SpendSummary? Spend = null,
    IReadOnlyList<DetailLine>? Details = null,
    string? AccountEmail = null)
{
    public IEnumerable<RateWindow> AllWindows
    {
        get
        {
            if (Primary is not null) yield return Primary;
            if (Secondary is not null) yield return Secondary;
            foreach (var w in Extra) yield return w;
        }
    }
}

/// <param name="CliName">Provider id understood by <c>codexbar usage -p</c>.</param>
/// <param name="IconName">Base name of the SVG under Assets/ProviderIcons.</param>
/// <param name="BrandColor">Meter colour (#RRGGBB); null follows the system text colour.</param>
public sealed record ProviderInfo(ProviderId Id, string DisplayName, string CliName, string IconName, string? BrandColor)
{
    /// <summary>Short label for the switcher tabs, which are ~70px wide.</summary>
    public string TabName => Id switch
    {
        ProviderId.OpenCode => "OC Zen",
        ProviderId.OpenCodeGo => "OC Go",
        _ => DisplayName,
    };
}

public static class Providers
{
    public static readonly IReadOnlyList<ProviderInfo> All =
    [
        new(ProviderId.Codex, "Codex", "codex", "codex", "#3B82F6"),
        new(ProviderId.Claude, "Claude", "claude", "claude", "#E8803A"),
        new(ProviderId.Cursor, "Cursor", "cursor", "cursor", "#22C55E"),
        new(ProviderId.Gemini, "Gemini", "gemini", "gemini", "#AB87EA"),
        new(ProviderId.Antigravity, "Antigravity", "antigravity", "antigravity", "#60BA7E"),
        new(ProviderId.OpenCode, "OpenCode Zen", "opencode", "opencode", "#EAB308"),
        new(ProviderId.OpenCodeGo, "OpenCode Go", "opencodego", "opencodego", "#EAB308"),
        new(ProviderId.Grok, "Grok", "grok", "grok", null),
        new(ProviderId.OpenRouter, "OpenRouter", "openrouter", "openrouter", "#6B7280"),
    ];

    public static ProviderInfo Get(ProviderId id) => All.First(p => p.Id == id);
}
