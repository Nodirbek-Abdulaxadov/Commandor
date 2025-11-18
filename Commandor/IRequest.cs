namespace Commandor;

/// <summary>
/// Request uchun marker interfeys (javobsiz)
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Generic request interfeysi (javob bilan)
/// </summary>
/// <typeparam name="TResponse">Javob tipi</typeparam>
public interface IRequest<out TResponse>
{
}
