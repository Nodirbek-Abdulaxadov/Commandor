using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Commandor uchun DependencyInjection extension metodlari
/// </summary>
public static class CommandorServiceCollectionExtensions
{
    /// <summary>
    /// Commandorni va handlerlarni DI containerga qo'shish.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Handlerlar joylashgan assemblylar</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCommandor(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Commandorni va Contextni singleton sifatida qo'shish
        services.AddSingleton<global::Commandor.CommandorContext>();
        services.AddScoped<global::Commandor.ICommandor, global::Commandor.Commandor>();

        // MemoryCache ni qo'shish (agar oldin qo'shilmagan bo'lsa)
        services.AddMemoryCache();

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Commandorni va handlerlarni qo'shish, TMarker assemblysi skanerlanadi.
    /// Bu usul AddCommandor() ning ishonchli muqobilidir, chunki u GetCallingAssembly() ga tayanmaydi.
    /// </summary>
    /// <typeparam name="TMarker">Skanerlanadigan assemblydan istalgan tip</typeparam>
    public static IServiceCollection AddCommandor<TMarker>(this IServiceCollection services)
        => services.AddCommandor(typeof(TMarker).Assembly);

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
        services.AddScoped<TService, TImplementation>();
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
                // Fix #11: TryAddTransient prevents double-registration when AddCommandorService is also called.
                services.TryAddTransient(@interface, handlerType.ImplementationType);
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
                // Fix #11: TryAddTransient prevents double-registration.
                services.TryAddTransient(@interface, handlerType.ImplementationType);
            }
        }
    }

    /// <summary>
    /// Auto-generated handlerlarni ro'yxatga olish.
    /// Fix #8: uses [GeneratedHandler] attribute for precise discovery instead of the fragile
    /// name-ends-with-"Handler" heuristic that could match unrelated classes.
    /// </summary>
    private static void RegisterGeneratedHandlers<TService>(IServiceCollection services)
    {
        var serviceType = typeof(TService);
        var assembly = serviceType.Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       t.IsDefined(typeof(global::Commandor.GeneratedHandlerAttribute), false))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           (i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<>) ||
                            i.GetGenericTypeDefinition() == typeof(global::Commandor.IRequestHandler<,>)))
                .ToList();

            foreach (var handlerInterface in handlerInterfaces)
            {
                // Fix #11: TryAddTransient prevents double-registration if both AddCommandor + AddCommandorService are called.
                services.TryAddTransient(handlerInterface, handlerType);
            }
        }
    }
}
