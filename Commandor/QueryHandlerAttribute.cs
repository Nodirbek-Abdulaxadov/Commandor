namespace Commandor;

/// <summary>
/// Marks a method on an <see cref="ICommandorService"/> interface as a query
/// handler. The source generator wraps the method in a caching proxy:
/// results are stored in <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
/// and keyed by the argument values. The cache is automatically invalidated
/// when a <see cref="CommandHandlerAttribute"/>-marked method on the same
/// service runs.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class QueryHandlerAttribute : Attribute
{
    /// <summary>Execution priority (lower runs first). Reserved for future ordering support.</summary>
    public double Priority { get; set; }

    /// <summary>Optional explicit handler name.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Absolute cache lifetime in seconds. <c>0</c> = no absolute expiration
    /// (the entry lives until the service is invalidated or evicted by memory pressure).
    /// </summary>
    public int CacheTtlSeconds { get; set; }
}
