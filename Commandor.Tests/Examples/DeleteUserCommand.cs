namespace Commandor.Tests.Examples;

/// <summary>
/// Foydalanuvchini o'chirish uchun command (javobsiz)
/// </summary>
public class DeleteUserCommand : IRequest
{
    public int UserId { get; set; }

    public DeleteUserCommand(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Foydalanuvchini o'chirish handlerni
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    public Task HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        // Bu yerda ma'lumotlar bazasidan o'chirish logikasi bo'lishi mumkin
        // Hozircha faqat simulatsiya qilamiz
        
        Console.WriteLine($"Foydalanuvchi o'chirilmoqda: ID = {request.UserId}");
        
        return Task.CompletedTask;
    }
}
