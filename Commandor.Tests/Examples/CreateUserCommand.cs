namespace Commandor.Tests.Examples;

/// <summary>
/// Foydalanuvchi yaratish uchun command
/// </summary>
public class CreateUserCommand : IRequest<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Foydalanuvchi yaratish handlerni
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    public Task<int> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        // Bu yerda ma'lumotlar bazasiga yozish logikasi bo'lishi mumkin
        // Hozircha faqat simulatsiya qilamiz
        
        Console.WriteLine($"Foydalanuvchi yaratilmoqda: {request.FirstName} {request.LastName} ({request.Email})");
        
        // Yangi foydalanuvchi ID sini qaytaramiz (simulatsiya)
        var userId = new Random().Next(1000, 9999);
        
        return Task.FromResult(userId);
    }
}
