using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace Commandor;

/// <summary>
/// Commandor context - manages cache invalidation tokens for services.
/// </summary>
public sealed class CommandorContext
{
    private readonly ConcurrentDictionary<Type, CancellationTokenSource> _tokens = new();

    /// <summary>
    /// Get change token for a specific service type.
    /// This token is used to depend on cache entries.
    /// </summary>
    public IChangeToken GetToken(Type serviceType)
    {
        var cts = _tokens.GetOrAdd(serviceType, _ => new CancellationTokenSource());
        return new CancellationChangeToken(cts.Token);
    }

    /// <summary>
    /// Invalidate all cache entries associated with the service type.
    /// </summary>
    public void Invalidate(Type serviceType)
    {
        if (_tokens.TryRemove(serviceType, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
