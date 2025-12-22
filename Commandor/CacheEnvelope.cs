#nullable enable

namespace Commandor;

/// <summary>
/// Internal cache envelope that stores the result value.
/// </summary>
public sealed class CacheEnvelope<T>
{
    public bool HasValue { get; set; }
    public T Value { get; set; } = default!;
}
