using Tokenbar.Models;

namespace Tokenbar.Services;

public interface IUsageFetcher
{
    ProviderId Provider { get; }

    Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken);
}
