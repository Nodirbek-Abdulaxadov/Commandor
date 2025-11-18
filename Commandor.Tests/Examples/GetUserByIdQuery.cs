namespace Commandor.Tests.Examples;

/// <summary>
/// Foydalanuvchi ma'lumotlari
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Foydalanuvchini ID bo'yicha olish uchun query
/// </summary>
public class GetUserByIdQuery : IRequest<UserDto?>
{
    public int UserId { get; set; }

    public GetUserByIdQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Foydalanuvchini ID bo'yicha olish handlerni
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public Task<UserDto?> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken = default)
    {
        // Bu yerda ma'lumotlar bazasidan o'qish logikasi bo'lishi mumkin
        // Hozircha faqat simulatsiya qilamiz
        
        if (request.UserId <= 0)
            return Task.FromResult<UserDto?>(null);

        var user = new UserDto
        {
            Id = request.UserId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        return Task.FromResult<UserDto?>(user);
    }
}
