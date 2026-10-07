using Tokenbar.Models;

namespace Tokenbar.Services;

public sealed class UsageStore
{
    private readonly Dictionary<ProviderId, IUsageFetcher> _fetchers;
    private readonly Dictionary<ProviderId, UsageSnapshot> _snapshots = new();
    private readonly HashSet<ProviderId> _loading = new();

    public UsageStore(IEnumerable<IUsageFetcher> fetchers, AppSettings settings, bool usingSampleData)
    {
        _fetchers = fetchers.ToDictionary(f => f.Provider);
        Settings = settings;
        UsingSampleData = usingSampleData;
    }

    public AppSettings Settings { get; }

    public bool UsingSampleData { get; }

    public IReadOnlyList<ProviderId> Enabled =>
        Providers.All.Select(p => p.Id).Where(id => _fetchers.ContainsKey(id) && Settings.IsEnabled(id)).ToList();

    /// <summary>The provider shown in the tray icon and, unless the overview is open, in the panel.</summary>
    public ProviderId Selected
    {
        get
        {
            var enabled = Enabled;
            var saved = Providers.All.FirstOrDefault(p => p.CliName == Settings.SelectedProvider)?.Id;
            if (saved is ProviderId id && enabled.Contains(id)) return id;
            return enabled.Count > 0 ? enabled[0] : ProviderId.Codex;
        }
        set
        {
            Settings.SelectedProvider = Providers.Get(value).CliName;
            Settings.Save();
        }
    }

    /// <summary>Raised on the caller's synchronization context after any provider updates.</summary>
    public event Action? Changed;

    public UsageSnapshot? Get(ProviderId id) => _snapshots.GetValueOrDefault(id);

    public bool IsLoading(ProviderId id) => _loading.Contains(id);

    public bool AnyLoading => _loading.Count > 0;

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
