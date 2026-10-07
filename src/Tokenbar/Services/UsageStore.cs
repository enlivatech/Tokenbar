using Tokenbar.Models;

namespace Tokenbar.Services;

public sealed class UsageStore
{
    private readonly Dictionary<ProviderId, IUsageFetcher> _fetchers;
    private readonly Dictionary<ProviderId, UsageSnapshot> _snapshots = new();
    private readonly HashSet<ProviderId> _loading = new();

    public UsageStore(IEnumerable<IUsageFetcher> fetchers, bool usingSampleData)
    {
        _fetchers = fetchers.ToDictionary(f => f.Provider);
        UsingSampleData = usingSampleData;
    }

    public bool UsingSampleData { get; }

    public IReadOnlyList<ProviderId> Enabled => Providers.All.Select(p => p.Id).Where(_fetchers.ContainsKey).ToList();

    public ProviderId Selected { get; set; } = ProviderId.Codex;

    /// <summary>Raised on the caller's synchronization context after any provider updates.</summary>
    public event Action? Changed;

    public UsageSnapshot? Get(ProviderId id) => _snapshots.GetValueOrDefault(id);

    public bool IsLoading(ProviderId id) => _loading.Contains(id);

    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        await Task.WhenAll(Enabled.Select(id => RefreshAsync(id, ct)));
    }

    public async Task RefreshAsync(ProviderId id, CancellationToken ct = default)
    {
        if (!_fetchers.TryGetValue(id, out var fetcher) || !_loading.Add(id)) return;
        Changed?.Invoke();
        try
        {
            var snapshot = await Task.Run(() => fetcher.FetchAsync(ct), ct);
            // Keep the last good numbers visible when a refresh fails; surface the error alongside.
            if (snapshot.Error is not null && _snapshots.TryGetValue(id, out var previous) && previous.Error is null)
                snapshot = previous with { Error = snapshot.Error };
            _snapshots[id] = snapshot;
        }
        finally
        {
            _loading.Remove(id);
            Changed?.Invoke();
        }
    }
}
