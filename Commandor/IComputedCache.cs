using System.Collections.Concurrent;
using System.Text.Json;
using LiteAPI.Cache;

namespace Commandor;

/// <summary>
/// Cache provider for computed values.
/// </summary>
public interface IComputedCache
{
    void Set<T>(string key, T value);
    T? Get<T>(string key);
    void Remove(string key);
    void Clear();
}

/// <summary>
/// LiteAPI.Cache implementation (GC-free Rust-backed cache).
/// Default cache provider for Commandor.
/// </summary>
public class LiteApiComputedCache : IComputedCache
{
    private static bool _initialized = false;
    private static readonly object _lock = new();
    
    public LiteApiComputedCache()
    {
        EnsureInitialized();
    }
    
    private static void EnsureInitialized()
    {
        if (_initialized)
            return;
            
        lock (_lock)
        {
            if (_initialized)
                return;
                
            JustCache.Initialize();
            _initialized = true;
        }
    }
    
    public void Set<T>(string key, T value)
    {
        if (value is not null)
        {
            var json = JsonSerializer.Serialize(value);
            JustCache.SetString(key, json);
        }
    }
    
    public T? Get<T>(string key)
    {
        try
        {
            var json = JustCache.GetString(key);
            if (json != null)
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            return default;
        }
        catch
        {
            return default;
        }
    }
    
    public void Remove(string key)
    {
        JustCache.Remove(key);
    }
    
    public void Clear()
    {
        JustCache.ClearAll();
    }
}

/// <summary>
/// High-performance in-memory cache for Commandor.
/// Thread-safe with ConcurrentDictionary.
/// Fallback if LiteAPI.Cache not available.
/// </summary>
public class CommandorMemoryCache : IComputedCache
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();
    
    public void Set<T>(string key, T value)
    {
        _cache[key] = value;
    }
    
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value is T typed ? typed : default;
        }
        return default;
    }
    
    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }
    
    public void Clear()
    {
        _cache.Clear();
    }
    
    public int Count => _cache.Count;
}
