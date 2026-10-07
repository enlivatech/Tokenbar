using System.Globalization;
using System.Text.Json;
using Tokenbar.Models;

namespace Tokenbar.Services;

/// <summary>
/// Gets usage from the Win-CodexBar Rust CLI (<c>codexbar usage -p &lt;id&gt; --json</c>), which owns
/// all provider auth, cookies and HTTP logic. This class only maps its JSON into our models.
/// </summary>
public sealed class CliUsageFetcher(ProviderId provider) : IUsageFetcher
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    public ProviderId Provider { get; } = provider;

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var info = Providers.Get(Provider);
        CliResult result;
        try
        {
            result = await CliRunner.RunAsync(["usage", "-p", info.CliName, "--json"], timeout: Timeout, ct: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ErrorSnapshot(ex.Message);
        }

        try
        {
            return Parse(Provider, result.Stdout);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return ErrorSnapshot(string.IsNullOrWhiteSpace(result.Stderr) ? ex.Message : result.Stderr.Trim());
        }
    }

    private UsageSnapshot ErrorSnapshot(string message) =>
        new(Provider, DateTimeOffset.Now, null, null, null, [], Error: message);

    /// <summary>Lane names the CLI JSON omits; matches macOS CodexBar's provider descriptors.</summary>
    private static readonly Dictionary<ProviderId, (string? Primary, string? Secondary, string? Model)> LaneLabels = new()
    {
        [ProviderId.Cursor] = ("Total", "Cursor", "Third Party"),
    };

    internal static UsageSnapshot Parse(ProviderId provider, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            root = root.EnumerateArray().FirstOrDefault();
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("CLI 没有返回数据");
        }
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            root = results.EnumerateArray().First();
        }

        if (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
        {
            var message = error.ValueKind == JsonValueKind.String
                ? error.GetString()
                : Str(error, "message") ?? error.ToString();
            return new UsageSnapshot(provider, DateTimeOffset.Now, null, null, null, [], Error: message);
        }

        var usage = root.GetProperty("usage");
        var labels = LaneLabels.GetValueOrDefault(provider);
        var primary = Window(usage, "primary", Str(usage, "primary_label") ?? labels.Primary, "Session");
        var secondary = Window(usage, "secondary", Str(usage, "secondary_label") ?? labels.Secondary, "Weekly");
        if (root.TryGetProperty("pace", out var pace) && pace.ValueKind == JsonValueKind.Object)
        {
            if (primary is not null && Pace(pace, "primary") is { } pp) primary = primary with { Pace = pp };
            if (secondary is not null && Pace(pace, "secondary") is { } sp) secondary = secondary with { Pace = sp };
        }

        var extra = new List<RateWindow>();
        if (Window(usage, "model_specific", labels.Model, "Model") is { } model) extra.Add(model);
        if (Window(usage, "tertiary", null, "Monthly") is { } tertiary) extra.Add(tertiary);
        if (usage.TryGetProperty("extra_rate_windows", out var named) && named.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in named.EnumerateArray())
            {
                if (item.TryGetProperty("window", out var w))
                    extra.Add(ToWindow(w, Str(item, "title") ?? Str(item, "id") ?? "Usage"));
            }
        }

        SpendSummary? spend = null;
        if (root.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Object)
        {
            var period = Str(cost, "period") ?? "";
            // "Token cost (metered, …)" is the usage priced at API rates, not money actually billed.
            bool apiValue = period.Contains("token cost", StringComparison.OrdinalIgnoreCase)
                || period.Contains("metered", StringComparison.OrdinalIgnoreCase);
            spend = new SpendSummary(
                Num(cost, "used") ?? 0,
                Num(cost, "limit"),
                Str(cost, "currency_symbol") ?? CurrencySymbol(Str(cost, "currency_code")),
                apiValue ? "" : period,
                Date(cost, "resets_at"),
                apiValue);
        }

        var details = new List<DetailLine>();
        if (root.TryGetProperty("details", out var detailArray) && detailArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in detailArray.EnumerateArray())
            {
                var title = Str(d, "title");
                var value = Str(d, "value");
                if (title is not null && value is not null) details.Add(new DetailLine(title, value));
            }
        }

        return new UsageSnapshot(
            provider,
            Date(usage, "updated_at") ?? DateTimeOffset.Now,
            PlanLabel.Format(provider, Str(usage, "login_method")),
            primary,
            secondary,
            extra,
            Spend: spend,
            Details: details,
            AccountEmail: Str(usage, "account_email"));
    }

    private static RateWindow? Window(JsonElement parent, string name, string? label, string fallbackTitle)
    {
        if (!parent.TryGetProperty(name, out var w) || w.ValueKind != JsonValueKind.Object) return null;
        return ToWindow(w, label ?? TitleFor(w) ?? fallbackTitle);
    }

    private static RateWindow ToWindow(JsonElement w, string title)
    {
        var resetsAt = Date(w, "resets_at");
        var description = Str(w, "reset_description");
        bool informational = w.TryGetProperty("is_informational", out var flag) && flag.ValueKind == JsonValueKind.True;
        // With a real timestamp the description is just a pre-rendered countdown; we format our own.
        var detail = informational || resetsAt is null ? description : null;
        return new RateWindow(title, Num(w, "used_percent") ?? 0, informational ? null : resetsAt, detail, informational);
    }

    private static UsagePace? Pace(JsonElement pace, string name)
    {
        if (!pace.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Object) return null;
        if (Num(p, "deltaPercent") is not double delta) return null;
        bool lasts = p.TryGetProperty("willLastToReset", out var l) && l.ValueKind == JsonValueKind.True;
        return new UsagePace(delta, lasts);
    }

    private static string? TitleFor(JsonElement w) => (int?)Num(w, "window_minutes") switch
    {
        300 => "Session",
        1440 => "Daily",
        10080 => "Weekly",
        >= 40000 and <= 46000 => "Monthly",
        _ => null,
    };

    private static string CurrencySymbol(string? code) => code switch
    {
        null or "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "CNY" or "JPY" => "¥",
        _ => code + " ",
    };

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static DateTimeOffset? Date(JsonElement e, string name) =>
        Str(e, name) is { } s && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
            ? d.ToLocalTime()
            : null;
}
