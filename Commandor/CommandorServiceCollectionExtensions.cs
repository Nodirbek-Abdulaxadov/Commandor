using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Commandor uchun DependencyInjection extension metodlari
/// </summary>
public static class CommandorServiceCollectionExtensions
{
    /// <summary>
    /// Commandorni va handlerlarni DI containerga qo'shish
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Handlerlar joylashgan assemblylar</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCommandor(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Commandorni singleton sifatida qo'shish
        services.AddSingleton<global::Commandor.ICommandor, global::Commandor.Commandor>();
        services.TryAddSingleton<global::Commandor.IComputedCache>(_ =>
        {
            var cache = CreateDefaultCache();
            global::Commandor.ServiceCacheRegistry.TrackCache(cache);
            return cache;
        });

        // Agar assemblylar berilmagan bo'lsa, chaqiruvchi assemblyni ishlatish
        var assembliesToScan = assemblies.Length > 0
            ? assemblies
            : new[] { Assembly.GetCallingAssembly() };

        // Handlerlarni topish va ro'yxatga olish
        foreach (var assembly in assembliesToScan)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Commandor service'ni va uning handlerlarini qo'shish
    /// </summary>
    /// <typeparam name="TService">Service interfeysi</typeparam>
    /// <typeparam name="TImplementation">Service implementatsiyasi</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCommandorService<TService, TImplementation>(this IServiceCollection services)
        where TService : class, global::Commandor.ICommandorService
        where TImplementation : class, TService
    {
        // Service'ni qo'shish
        services.AddScoped<TService, TImplementation>();

        // Auto-generated handlerlarni qo'shish
        RegisterGeneratedHandlers<TService>(services);

        return services;
    }

    /// <summary>
    /// Assemblydagi barcha handlerlarni topish va ro'yxatga olish
    /// </summary>
    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        // IRequestHandler<TRequest> implementatsiyalarini topish (javobsiz)
        var requestHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                ImplementationType = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<>) &&
                               i.GenericTypeArguments.Length == 1)
            })
            .Where(x => x.Interfaces.Any());

        foreach (var handlerType in requestHandlerTypes)
        {
            foreach (var @interface in handlerType.Interfaces)
            {
                services.AddTransient(@interface, handlerType.ImplementationType);
            }
        }

        // IRequestHandler<TRequest, TResponse> implementatsiyalarini topish (javobli)
        var requestResponseHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                ImplementationType = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<,>) &&
                               i.GenericTypeArguments.Length == 2)
            })
            .Where(x => x.Interfaces.Any());

        foreach (var handlerType in requestResponseHandlerTypes)
        {
            foreach (var @interface in handlerType.Interfaces)
            {
                services.AddTransient(@interface, handlerType.ImplementationType);
            }
        }
    }

    /// <summary>
    /// Auto-generated handlerlarni ro'yxatga olish
    /// </summary>
    private static void RegisterGeneratedHandlers<TService>(IServiceCollection services)
    {
        var serviceType = typeof(TService);
        var assembly = serviceType.Assembly;

        // Service namespace'dagi barcha auto-generated handlerlarni topish
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       t.Namespace == serviceType.Namespace &&
                       t.Name.EndsWith("Handler"))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // Handler'ning implement qilgan interfeyslari
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           (i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<>) ||
                            i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<,>)))
                .ToList();

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddTransient(handlerInterface, handlerType);
            }
        }
    }

    private static global::Commandor.IComputedCache CreateDefaultCache()
    {
        try
        {
            return new global::Commandor.LiteApiComputedCache();
        }
        catch (DllNotFoundException)
        {
        }
        catch (TypeInitializationException ex) when (ex.InnerException is DllNotFoundException)
        {
        }

        return new global::Commandor.CommandorMemoryCache();
    }
}
