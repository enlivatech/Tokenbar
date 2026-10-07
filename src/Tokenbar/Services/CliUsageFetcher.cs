using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Tokenbar.Models;

namespace Tokenbar.Services;

/// <summary>
/// Gets usage from the Win-CodexBar Rust CLI (<c>codexbar usage -p &lt;id&gt; --json</c>), which owns
/// all provider auth, cookies and HTTP logic. This class only maps its JSON into our models.
/// </summary>
public sealed class CliUsageFetcher(ProviderId provider, string cliPath) : IUsageFetcher
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    public ProviderId Provider { get; } = provider;

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var info = Providers.Get(Provider);
        string stdout, stderr;
        try
        {
            (stdout, stderr) = await RunAsync(cliPath, ["usage", "-p", info.CliName, "--json", "--no-color"], cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ErrorSnapshot(ex.Message);
        }

        try
        {
            return Parse(Provider, stdout);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            var message = string.IsNullOrWhiteSpace(stderr) ? ex.Message : stderr.Trim();
            return ErrorSnapshot(message);
        }
    }

    private UsageSnapshot ErrorSnapshot(string message) =>
        new(Provider, DateTimeOffset.Now, null, null, null, [], Error: message);

    private static async Task<(string Stdout, string Stderr)> RunAsync(string exe, string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"无法启动 {exe}");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            if (ct.IsCancellationRequested) throw;
            throw new TimeoutException("读取用量超时");
        }
        return (await stdoutTask, await stderrTask);
    }

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
        var primary = Window(usage, "primary", Str(usage, "primary_label"), "Session");
        var secondary = Window(usage, "secondary", Str(usage, "secondary_label"), "Weekly");
        if (root.TryGetProperty("pace", out var pace) && pace.ValueKind == JsonValueKind.Object)
        {
            if (primary is not null && Pace(pace, "primary") is { } pp) primary = primary with { Pace = pp };
            if (secondary is not null && Pace(pace, "secondary") is { } sp) secondary = secondary with { Pace = sp };
        }

        var extra = new List<RateWindow>();
        if (Window(usage, "model_specific", null, "Model") is { } model) extra.Add(model);
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
            spend = new SpendSummary(
                Num(cost, "used") ?? 0,
                Num(cost, "limit"),
                Str(cost, "currency_symbol") ?? CurrencySymbol(Str(cost, "currency_code")),
                Str(cost, "period") ?? "",
                Date(cost, "resets_at"));
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
            Str(usage, "login_method"),
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
