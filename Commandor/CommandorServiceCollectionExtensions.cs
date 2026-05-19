using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Commandor DI registration.
///
/// Typical setup (one app, three calls):
///
/// <code>
/// services.AddCommandor&lt;Program&gt;();                      // mediator + memory cache + IRequestHandler scan
/// services.AddCommandorService&lt;ITodoService, TodoService&gt;();  // one per service
/// services.AddAppCommandor();                             // generated subclass with service properties
/// </code>
///
/// <c>AddAppCommandor()</c> is emitted by the source generator and only
/// exists once the project defines at least one <c>[QueryHandler]</c>
/// method on an <c>ICommandorService</c>.
/// </summary>
public static class CommandorServiceCollectionExtensions
{
    /// <summary>
    /// Register the mediator, the memory cache, and scan supplied
    /// assemblies for <see cref="global::Commandor.IRequestHandler{T}"/> /
    /// <see cref="global::Commandor.IRequestHandler{T, R}"/> command
    /// handlers the user wrote by hand.
    /// </summary>
    public static IServiceCollection AddCommandor(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<global::Commandor.CommandorContext>();
        services.AddMemoryCache();

        // Default mediator. AddAppCommandor() — if called — overrides
        // these registrations with the generated subclass.
        services.AddScoped<global::Commandor.Commandor>();
        services.AddScoped<global::Commandor.ICommandor>(sp =>
            sp.GetRequiredService<global::Commandor.Commandor>());

        foreach (var assembly in assemblies)
            RegisterHandlers(services, assembly);

        return services;
    }

    /// <summary>Scan <typeparamref name="TMarker"/>'s assembly for handlers.</summary>
    public static IServiceCollection AddCommandor<TMarker>(this IServiceCollection services)
        => services.AddCommandor(typeof(TMarker).Assembly);

    /// <summary>
    /// Register a Commandor service. If the source generator emitted a
    /// <c>[GeneratedProxy(typeof(TService))]</c> cached proxy for it
    /// (because the interface has at least one <c>[QueryHandler]</c>
    /// method), wire <typeparamref name="TService"/> to that proxy so
    /// query reads are transparently cached.
    /// </summary>
    public static IServiceCollection AddCommandorService<TService, TImplementation>(this IServiceCollection services)
        where TService : class, global::Commandor.ICommandorService
        where TImplementation : class, TService
    {
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

        return services;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Type?> _proxyCache = new();

    private static Type? ResolveProxyType<TService>()
    {
        return _proxyCache.GetOrAdd(typeof(TService), serviceType =>
        {
            // Search all loaded assemblies — the proxy can live in any
            // referencing project (typically the same one as the service
            // interface, but the generator places it under the interface's
            // namespace if non-global).
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }

                var hit = types.FirstOrDefault(t =>
                    t.IsClass && !t.IsAbstract &&
                    t.GetCustomAttribute<global::Commandor.GeneratedProxyAttribute>() is { } attr &&
                    attr.ServiceType == serviceType);
                if (hit is not null) return hit;
            }
            return null;
        });
    }

    /// <summary>
    /// Register user-written <see cref="global::Commandor.IRequestHandler{T}"/> /
    /// <see cref="global::Commandor.IRequestHandler{T, R}"/> implementations.
    /// Uses <c>TryAddTransient</c> so multiple calls don't double-register.
    /// </summary>
    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var allTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var t in allTypes)
        {
            foreach (var i in t.GetInterfaces())
            {
                if (!i.IsGenericType) continue;
                var def = i.GetGenericTypeDefinition();
                if (def == typeof(global::Commandor.IRequestHandler<>) ||
                    def == typeof(global::Commandor.IRequestHandler<,>))
                {
                    services.TryAddTransient(i, t);
                }
            }
        }
    }
}
