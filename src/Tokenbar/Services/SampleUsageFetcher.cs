using Tokenbar.Models;

namespace Tokenbar.Services;

/// <summary>Placeholder data until the real per-provider fetchers land.</summary>
public sealed class SampleUsageFetcher(ProviderId provider) : IUsageFetcher
{
    public ProviderId Provider { get; } = provider;

    public Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var seed = (int)Provider * 37 % 100;
        var snapshot = Provider switch
        {
            ProviderId.Claude => new UsageSnapshot(
                Provider, now, "Max",
                new RateWindow("Session", 2, now.AddHours(3).AddMinutes(53)),
                new RateWindow("Weekly", 3, now.AddDays(3).AddHours(20), Pace: new UsagePace(-42, true)),
                [new RateWindow("Sonnet", 0)],
                new CostSummary(0.04m, 15_000, 254.24m, 218_000_000)),
            ProviderId.OpenRouter => new UsageSnapshot(
                Provider, now, null,
                new RateWindow("Credits", 37, null, "$ 6.30 / $ 10.00"),
                null, []),
            _ => new UsageSnapshot(
                Provider, now, "Pro",
                new RateWindow("Session", seed, now.AddHours(1 + seed % 4).AddMinutes(seed % 60)),
                new RateWindow("Weekly", (seed * 3) % 100, now.AddDays(1 + seed % 6).AddHours(seed % 24)),
                []),
        };
        return Task.FromResult(snapshot);
    }
}
