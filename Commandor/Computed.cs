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
