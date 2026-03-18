namespace Commandor;

/// <summary>
/// Commandor interfeysi - asosiy mediator
/// </summary>
public interface ICommandor
{
    /// <summary>
    /// Javobsiz requestni yuborish
    /// </summary>
    /// <typeparam name="TRequest">Request tipi</typeparam>
    /// <param name="request">Request obyekti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>
    /// Javobli requestni yuborish
    /// </summary>
    /// <typeparam name="TResponse">Javob tipi</typeparam>
    /// <param name="request">Request obyekti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Javob obyekti</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query natijasini olish — SendAsync ning semantik muqobili,
    /// faqat o'qish (GET) operatsiyalari uchun mo'ljallangan.
    /// </summary>
    /// <typeparam name="TResponse">Javob tipi</typeparam>
    /// <param name="request">Query request obyekti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Javob obyekti</returns>
    Task<TResponse> GetAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Servisga tegishli barcha keshlarni tozalash (Invalidate).
    /// </summary>
    /// <typeparam name="TService">Servis interfeysi (masalan IUserService)</typeparam>
    void Invalidate<TService>();

    /// <summary>
    /// Servisga tegishli barcha keshlarni tozalash (Asinxron wrapper).
    /// </summary>
    /// <typeparam name="TService">Servis interfeysi</typeparam>
    Task InvalidateAsync<TService>(CancellationToken cancellationToken = default);
}
