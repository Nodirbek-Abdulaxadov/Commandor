namespace Commandor;

/// <summary>
/// Commandor service interfeysi - [CommandHandler] attribute'li metodlar uchun marker
/// </summary>
public interface ICommandorService
{
    // Marker interface - command handlerlar uchun servislarni belgilaydi

    /// <summary>
    /// Invalidate cache entries that belong to this service type.
    /// </summary>
    void InvalidateServiceCache() => ServiceCacheRegistry.InvalidateService(GetType());

    /// <summary>
    /// Invalidate cache entries for a specific service type.
    /// </summary>
    static void InvalidateServiceCache<TService>() where TService : ICommandorService
        => ServiceCacheRegistry.InvalidateService(typeof(TService));

    /// <summary>
    /// Invalidate cache entries for all services.
    /// </summary>
    static void InvalidateAllCaches() => ServiceCacheRegistry.InvalidateAll();
}

public static class CommandorServiceExtensions
{
    /// <summary>
    /// Extension wrapper to call service-scoped invalidation from implementations.
    /// </summary>
    public static void InvalidateServiceCache(this ICommandorService service)
        => ServiceCacheRegistry.InvalidateService(service.GetType());
}
