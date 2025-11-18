namespace Commandor;

/// <summary>
/// Request handler interfeysi (javobsiz)
/// </summary>
/// <typeparam name="TRequest">Request tipi</typeparam>
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Requestni asenkron ishlov berish
    /// </summary>
    /// <param name="request">Request obyekti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request handler interfeysi (javob bilan)
/// </summary>
/// <typeparam name="TRequest">Request tipi</typeparam>
/// <typeparam name="TResponse">Javob tipi</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Requestni asenkron ishlov berish va javob qaytarish
    /// </summary>
    /// <param name="request">Request obyekti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Javob obyekti</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
