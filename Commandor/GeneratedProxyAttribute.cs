namespace Commandor;

/// <summary>
/// Marks a class as a Commandor-generated cached proxy for a specific
/// <see cref="ICommandorService"/> interface. When you call
/// <c>AddCommandorService&lt;TService, TImpl&gt;()</c>, the proxy is wired up
/// transparently so that every <c>[QueryHandler]</c> method on
/// <c>TService</c> is auto-cached and every <c>[CommandHandler]</c> method
/// auto-invalidates the cache — directly through the injected service, with
/// no mediator boilerplate at the call site.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedProxyAttribute : Attribute
{
    /// <summary>The service interface this proxy decorates.</summary>
    public Type ServiceType { get; }

    public GeneratedProxyAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }
}
