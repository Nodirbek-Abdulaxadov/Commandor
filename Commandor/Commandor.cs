using System;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor;

/// <summary>
/// Commandor - asosiy mediator implementatsiyasi
/// </summary>
public class Commandor : ICommandor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandorContext _context;

    public Commandor(IServiceProvider serviceProvider, CommandorContext context)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Javobsiz requestni yuborish
    /// </summary>
    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handlerType = typeof(IRequestHandler<>).MakeGenericType(typeof(TRequest));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"Handler topilmadi: {typeof(TRequest).Name}");

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest>.HandleAsync));
        if (method == null)
            throw new InvalidOperationException($"HandleAsync metodi topilmadi: {handlerType.Name}");

        var task = (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Javobli requestni yuborish
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"Handler topilmadi: {requestType.Name}");

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync));
        if (method == null)
            throw new InvalidOperationException($"HandleAsync metodi topilmadi: {handlerType.Name}");

        var task = (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        return await task.ConfigureAwait(false);
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
