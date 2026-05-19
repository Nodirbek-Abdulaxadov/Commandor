namespace Commandor;

/// <summary>
/// Commandor — CQRS mediator. Two access patterns:
///
/// <list type="bullet">
/// <item><description>
/// <b>Commands</b> are dispatched by instance:
/// <c>await commandor.SendAsync(new CreateTodoCommand("buy milk"));</c>.
/// Each command class implements <see cref="IRequest"/> or
/// <see cref="IRequest{T}"/>, and has a matching
/// <see cref="IRequestHandler{T}"/> or <see cref="IRequestHandler{T, R}"/>
/// that the host application owns.
/// </description></item>
/// <item><description>
/// <b>Queries</b> are called as methods on auto-generated service
/// properties of the host's <c>AppCommandor</c> (a subclass of
/// <c>Commandor</c> that the source generator emits, one property per
/// interface decorated with <see cref="ICommandorService"/> and
/// <c>[QueryHandler]</c>):
/// <c>var todo = await commandor.TodoService.GetTodoByIdAsync(7);</c>.
/// Each <c>[QueryHandler]</c> call goes through the generated cached
/// proxy and is transparently memoized.
/// </description></item>
/// </list>
///
/// Cache invalidation is manual — call <see cref="Invalidate{TService}"/>
/// from inside a command handler whenever its writes should drop the
/// affected service's cached query results.
/// </summary>
public interface ICommandor
{
    /// <summary>Send a fire-and-forget command (no response).</summary>
    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>Send a command and await its response.</summary>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>Invalidate all cached query results for <typeparamref name="TService"/>.</summary>
    void Invalidate<TService>();

    /// <summary>Async wrapper around <see cref="Invalidate{TService}"/>.</summary>
    Task InvalidateAsync<TService>(CancellationToken cancellationToken = default);
}
