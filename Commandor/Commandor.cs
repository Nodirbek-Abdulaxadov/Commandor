using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor;

/// <summary>
/// Commandor - asosiy mediator implementatsiyasi
/// </summary>
public class Commandor : ICommandor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandorContext _context;

    // Cache MakeGenericType + GetMethod results so reflection cost is paid only once per request type.
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), (Type HandlerType, MethodInfo Method)> _handlerCache = new();

    public Commandor(IServiceProvider serviceProvider, CommandorContext context)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Javobsiz requestni yuborish — no reflection; resolved via generic DI lookup.
    /// </summary>
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

    /// <summary>
    /// Javobli requestni yuborish — MethodInfo cached per (requestType, responseType) pair.
    /// </summary>
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

    /// <summary>
    /// SendAsync ning semantik muqobili — faqat query (GET) operatsiyalar uchun.
    /// </summary>
    public Task<TResponse> GetAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => SendAsync(request, cancellationToken);

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
