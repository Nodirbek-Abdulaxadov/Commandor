namespace Commandor;

/// <summary>
/// Metodga avtomatik command handler yaratish uchun attribute.
/// Bu attribute qo'yilgan metodlar uchun Source Generator IRequestHandler yaratadi.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandHandlerAttribute : Attribute
{
    /// <summary>
    /// Handler prioriteti (kichik qiymat birinchi ishlaydi)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Handler nomi (default: metod nomi)
    /// </summary>
    public string? Name { get; set; }

    public CommandHandlerAttribute()
    {
    }
}
