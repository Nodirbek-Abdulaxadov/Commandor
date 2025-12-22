using System.Collections.Concurrent;
using System.Text.Json;

namespace Commandor;

/// <summary>
/// Computed value implementation.
/// Stores method call result and manages invalidation.
/// </summary>
public class Computed<T> : IComputed<T>
{
    private static ulong _nextVersion = 0;
    private ConsistencyState _state = ConsistencyState.Consistent;
    private readonly TaskCompletionSource _whenInvalidatedSource = new();
    
    public ulong Version { get; }
    public string CacheKey { get; }
    public T? Value { get; private set; }
    public Exception? Error { get; private set; }
    public bool HasValue => Error == null;
    public bool HasError => Error != null;
    
    public ConsistencyState ConsistencyState => _state;
    
    public event Action<IComputed>? Invalidated;
    
    // Dependencies tracking (Fusion pattern)
    private readonly HashSet<IComputed> _dependencies = new();
    private readonly HashSet<IComputed> _dependents = new();
    
    public Computed(string cacheKey, T? value)
    {
        Version = Interlocked.Increment(ref _nextVersion);
        CacheKey = cacheKey;
        Value = value;
        Error = null;
    }
    
    public Computed(string cacheKey, Exception error)
    {
        Version = Interlocked.Increment(ref _nextVersion);
        CacheKey = cacheKey;
        Value = default;
        Error = error;
    }
    
    public void Invalidate()
    {
        if (_state == ConsistencyState.Invalidated)
            return;
            
        lock (this)
        {
            if (_state == ConsistencyState.Invalidated)
                return;
                
            _state = ConsistencyState.Invalidated;
            
            // Invalidate all dependents (cascading invalidation - Fusion pattern)
            foreach (var dependent in _dependents.ToArray())
            {
                dependent.Invalidate();
            }
            
            // Fire event
            Invalidated?.Invoke(this);
            _whenInvalidatedSource.TrySetResult();
            
            // Cleanup
            _dependencies.Clear();
            _dependents.Clear();
        }
    }
    
    public Task WhenInvalidated(CancellationToken cancellationToken = default)
    {
        if (_state == ConsistencyState.Invalidated)
            return Task.CompletedTask;
            
        if (cancellationToken.CanBeCanceled)
        {
            return _whenInvalidatedSource.Task.WaitAsync(cancellationToken);
        }
        
        return _whenInvalidatedSource.Task;
    }
    
    public bool IsConsistent() => _state == ConsistencyState.Consistent;
    
    // Dependency tracking methods (Fusion pattern)
    internal void AddDependency(IComputed dependency)
    {
        lock (this)
        {
            _dependencies.Add(dependency);
            if (dependency is Computed<T> typed)
            {
                lock (typed)
                {
                    typed._dependents.Add(this);
                }
            }
        }
    }
}

/// <summary>
/// Computed registry - stores all computed values.
/// Similar to Fusion's ComputedRegistry.
/// </summary>
public static class ComputedRegistry
{
    private static readonly ConcurrentDictionary<string, IComputed> _registry = new();
    
    public static void Register(IComputed computed)
    {
        _registry[computed.CacheKey] = computed;
    }
    
    public static IComputed? Get(string cacheKey)
    {
        _registry.TryGetValue(cacheKey, out var computed);
        return computed;
    }
    
    public static IComputed<T>? Get<T>(string cacheKey)
    {
        return Get(cacheKey) as IComputed<T>;
    }
    
    public static void Remove(string cacheKey)
    {
        _registry.TryRemove(cacheKey, out _);
    }
    
    public static void Clear()
    {
        _registry.Clear();
    }
}

/// <summary>
/// Computed value that is tied to a specific ICommandorService implementation.
/// </summary>
public sealed class ServiceComputed<T> : Computed<T>
{
    public Type ServiceType { get; }

    public ServiceComputed(Type serviceType, string cacheKey, T? value) : base(cacheKey, value)
    {
        ServiceType = serviceType;
    }

    public ServiceComputed(Type serviceType, string cacheKey, Exception error) : base(cacheKey, error)
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Registry for service-scoped computed values and cache invalidation.
/// </summary>
public static class ServiceCacheRegistry
{
    private sealed record CacheEntry(IComputed Computed, IComputedCache Cache);

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, CacheEntry>> _serviceEntries = new();
    private static readonly ConcurrentDictionary<IComputedCache, byte> _knownCaches = new();

    public static void TrackCache(IComputedCache cache)
    {
        _knownCaches.TryAdd(cache, 0);
    }

    public static bool TryGet<T>(Type serviceType, string cacheKey, out ServiceComputed<T>? computed)
    {
        computed = null;

        if (_serviceEntries.TryGetValue(serviceType, out var entries) &&
            entries.TryGetValue(cacheKey, out var entry))
        {
            computed = entry.Computed as ServiceComputed<T>;
            if (computed != null && computed.IsConsistent())
            {
                return true;
            }

            entries.TryRemove(cacheKey, out _);
            ComputedRegistry.Remove(cacheKey);
        }

        return false;
    }

    public static ServiceComputed<T> Register<T>(Type serviceType, string cacheKey, T? value, IComputedCache cache)
    {
        TrackCache(cache);

        var computed = new ServiceComputed<T>(serviceType, cacheKey, value);
        var entries = _serviceEntries.GetOrAdd(serviceType, _ => new ConcurrentDictionary<string, CacheEntry>());

        entries[cacheKey] = new CacheEntry(computed, cache);
        ComputedRegistry.Register(computed);

        return computed;
    }

    public static void InvalidateService(Type serviceType)
    {
        if (!_serviceEntries.TryRemove(serviceType, out var entries))
            return;

        foreach (var entry in entries.ToArray())
        {
            entry.Value.Computed.Invalidate();
            entry.Value.Cache.Remove(entry.Key);
            ComputedRegistry.Remove(entry.Key);
        }
    }

    public static void InvalidateAll()
    {
        foreach (var serviceType in _serviceEntries.Keys.ToArray())
        {
            InvalidateService(serviceType);
        }

        _serviceEntries.Clear();

        foreach (var cache in _knownCaches.Keys)
        {
            cache.Clear();
        }

        ComputedRegistry.Clear();
        _knownCaches.Clear();
    }
}
