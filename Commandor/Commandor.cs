using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor;

/// <summary>
/// Default <see cref="ICommandor"/> implementation. Marked <c>public</c>
/// and non-sealed so the source generator can emit an
/// <c>AppCommandor : Commandor</c> subclass with one property per
/// registered <see cref="ICommandorService"/>.
/// </summary>
public class Commandor : ICommandor
{
    /// <summary>
    /// Service provider used to resolve handlers and (for the generated
    /// <c>AppCommandor</c> subclass) cached service proxies. Marked
    /// <c>protected</c> so subclasses can hand out service properties
    /// like <c>commandor.TodoService</c>.
    /// </summary>
    protected readonly IServiceProvider _serviceProvider;
    protected readonly CommandorContext _context;

    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), (Type HandlerType, MethodInfo Method)> _handlerCache = new();

    public Commandor(IServiceProvider serviceProvider, CommandorContext context)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handler = _serviceProvider.GetService<IRequestHandler<TRequest>>();
        if (handler == null)
            throw new InvalidOperationException($"Handler topilmadi: {typeof(TRequest).Name}");

        await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var key = (requestType, typeof(TResponse));

        if (!_handlerCache.TryGetValue(key, out var cached))
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))
                         ?? throw new InvalidOperationException($"HandleAsync metodi topilmadi: {handlerType.Name}");
            cached = (handlerType, method);
            _handlerCache.TryAdd(key, cached);
        }

        var handler = _serviceProvider.GetService(cached.HandlerType);
        if (handler == null)
            throw new InvalidOperationException($"Handler topilmadi: {requestType.Name}");

        return await ((Task<TResponse>)cached.Method.Invoke(handler, [request, cancellationToken])!).ConfigureAwait(false);
    }

    public void Invalidate<TService>()
    {
        _context.Invalidate(typeof(TService));
    }

    public Task InvalidateAsync<TService>(CancellationToken cancellationToken = default)
    {
        Invalidate<TService>();
        return Task.CompletedTask;
    }
}
