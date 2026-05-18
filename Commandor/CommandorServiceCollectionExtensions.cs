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
    /// Registers a Commandor service and wires up its auto-generated cached proxy.
    /// <para>
    /// When the source generator emits a <c>[GeneratedProxy(typeof(TService))]</c> class
    /// (the default for any interface with <c>[QueryHandler]</c> / <c>[CommandHandler]</c>
    /// members), <typeparamref name="TService"/> resolves to that proxy. Every
    /// <c>[QueryHandler]</c> method is then auto-cached and every
    /// <c>[CommandHandler]</c> method auto-invalidates the cache — without any
    /// mediator boilerplate at the call site. The concrete <typeparamref name="TImplementation"/>
    /// is also registered (scoped) so the proxy can resolve it.
    /// </para>
    /// <para>
    /// The legacy mediator path (<c>ICommandor.SendAsync(...)</c> and the
    /// <c>commandor.MethodNameAsync(...)</c> extension methods) is registered alongside
    /// the proxy for backward compatibility.
    /// </para>
    /// </summary>
    /// <typeparam name="TService">Service interface (must inherit <see cref="global::Commandor.ICommandorService"/>).</typeparam>
    /// <typeparam name="TImplementation">Concrete implementation type.</typeparam>
    public static IServiceCollection AddCommandorService<TService, TImplementation>(this IServiceCollection services)
        where TService : class, global::Commandor.ICommandorService
        where TImplementation : class, TService
    {
        // Concrete impl must be resolvable on its own so the proxy can ask DI for it.
        services.AddScoped<TImplementation>();

        var proxyType = ResolveProxyType<TService>();
        if (proxyType is not null)
        {
            services.AddScoped<TService>(sp =>
            {
                var impl = sp.GetRequiredService<TImplementation>();
                return (TService)ActivatorUtilities.CreateInstance(sp, proxyType, impl);
            });
        }
        else
        {
            services.AddScoped<TService>(sp => sp.GetRequiredService<TImplementation>());
        }

        // Mediator-path handlers stay registered so commandor.SendAsync / commandor.MethodNameAsync still work.
        RegisterGeneratedHandlers<TService>(services);
        return services;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Type?> _proxyCache = new();

    private static Type? ResolveProxyType<TService>()
    {
        return _proxyCache.GetOrAdd(typeof(TService), serviceType =>
        {
            return serviceType.Assembly.GetTypes()
                .FirstOrDefault(t =>
                    t.IsClass && !t.IsAbstract &&
                    t.GetCustomAttribute<global::Commandor.GeneratedProxyAttribute>() is { } attr &&
                    attr.ServiceType == serviceType);
        });
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
