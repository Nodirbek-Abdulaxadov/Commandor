namespace Commandor;

/// <summary>
/// Query handler methodini belgilaydi (GET operatsiyalar) va natijani avtomatik cache'laydi.
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
    /// Hozircha dekorativ property, tez orada real TTL bilan ishlaydi.
    /// </summary>
    public int CacheTtlSeconds { get; set; }
}
