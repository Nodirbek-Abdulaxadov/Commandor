namespace Commandor;

/// <summary>
/// Query handler methodini belgilaydi (GET operatsiyalar).
/// Keyinchalik caching qo'shiladi (ActualLab.Fusion ComputeMethod kabi).
/// Source Generator tomonidan IRequestHandler yaratiladi.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class QueryHandlerAttribute : Attribute
{
    /// <summary>
    /// Handler prioriteti (bajarilish tartibi).
    /// </summary>
    public double Priority { get; set; }

    /// <summary>
    /// Handler nomi (ixtiyoriy).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Cache TTL (Time To Live) sekundlarda.
    /// Default: 0 (caching o'chirilgan).
    /// TODO: Keyinchalik caching mexanizm qo'shiladi.
    /// </summary>
    public int CacheTtlSeconds { get; set; }
}
