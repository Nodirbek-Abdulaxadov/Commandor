namespace Commandor;

/// <summary>
/// Computed value - method call natijasi va uning holati.
/// Fusion IComputed pattern asosida.
/// </summary>
public interface IComputed
{
    /// <summary>
    /// Unique version for this computed instance.
    /// </summary>
    ulong Version { get; }
    
    /// <summary>
    /// Consistency state: Consistent | Invalidated.
    /// </summary>
    ConsistencyState ConsistencyState { get; }
    
    /// <summary>
    /// Method call key (serviceType, methodName, arguments).
    /// </summary>
    string CacheKey { get; }
    
    /// <summary>
    /// Event fired when this computed is invalidated.
    /// </summary>
    event Action<IComputed>? Invalidated;
    
    /// <summary>
    /// Invalidate this computed value.
    /// </summary>
    void Invalidate();
    
    /// <summary>
    /// Wait until this computed is invalidated.
    /// </summary>
    Task WhenInvalidated(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if this computed is still consistent.
    /// </summary>
    bool IsConsistent();
}

/// <summary>
/// Computed value with typed result.
/// </summary>
public interface IComputed<T> : IComputed
{
    /// <summary>
    /// Cached value (if HasValue is true).
    /// </summary>
    T? Value { get; }
    
    /// <summary>
    /// Error (if HasError is true).
    /// </summary>
    Exception? Error { get; }
    
    /// <summary>
    /// Whether this computed has a value (not error).
    /// </summary>
    bool HasValue { get; }
    
    /// <summary>
    /// Whether this computed has an error.
    /// </summary>
    bool HasError { get; }
}

/// <summary>
/// Consistency state of computed value.
/// </summary>
public enum ConsistencyState
{
    /// <summary>
    /// Value is consistent and can be used.
    /// </summary>
    Consistent,
    
    /// <summary>
    /// Value is invalidated and should be recomputed.
    /// </summary>
    Invalidated
}
