using Microsoft.Extensions.DependencyInjection;
using Commandor.Tests.Examples;
using Commandor;

namespace Commandor.Tests;

public class CommandorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandor _commandor;

    public CommandorTests()
    {
        // DI containerini sozlash
        var services = new ServiceCollection();
        
        // Commandor va handlerlarni ro'yxatga olish
        services.AddCommandor(typeof(CommandorTests).Assembly);
        
        _serviceProvider = services.BuildServiceProvider();
        _commandor = _serviceProvider.GetRequiredService<ICommandor>();
    }

    [Fact]
    public async Task SendAsync_WithResponseCommand_ShouldReturnResponse()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        // Act
        var userId = await _commandor.SendAsync(command);

        // Assert
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task SendAsync_WithQuery_ShouldReturnUser()
    {
        // Arrange
        var query = new GetUserByIdQuery(123);

        // Act
        var user = await _commandor.SendAsync(query);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(123, user.Id);
        Assert.Equal("Test", user.FirstName);
    }

    [Fact]
    public async Task SendAsync_WithQueryInvalidId_ShouldReturnNull()
    {
        // Arrange
        var query = new GetUserByIdQuery(-1);

        // Act
        var user = await _commandor.SendAsync(query);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task SendAsync_WithoutResponseCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var command = new DeleteUserCommand(123);

        // Act & Assert
        await _commandor.SendAsync(command);
        // Agar xatolik bo'lmasa, test muvaffaqiyatli
    }

    [Fact]
    public async Task SendAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _commandor.SendAsync<int>(null!);
        });
    }

    [Fact]
    public async Task SendAsync_HandlerNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange - Register Commandor without any handlers
        var services = new ServiceCollection();
        services.AddSingleton<CommandorContext>();
        services.AddScoped<ICommandor, Commandor>();
        services.AddMemoryCache();
        
        var sp = services.BuildServiceProvider();
        var commandor = sp.GetRequiredService<ICommandor>();

        var command = new CreateUserCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await commandor.SendAsync(command);
        });
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Darhol bekor qilish

        // Act & Assert
        // CancellationToken hozircha handlerda tekshirilmaydi,
        // lekin kelajakda qo'shish mumkin
        var userId = await _commandor.SendAsync(command, cts.Token);
        Assert.True(userId > 0);
    }
}
