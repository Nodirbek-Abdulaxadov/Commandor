using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

/// <summary>
/// Tests for caching mechanism (Fusion pattern).
/// </summary>
public class CachingTests
{
    [Fact]
    public void CacheKeyBuilder_WithPrimitives_ShouldBuildCorrectKey()
    {
        // Arrange & Act
        var key = CacheKeyBuilder.Build(typeof(TestService), "GetProduct", 123, "test");

        // Assert
        Assert.Equal("TestService.GetProduct(123, \"test\")", key);
    }

    [Fact]
    public void CacheKeyBuilder_WithNull_ShouldHandleNull()
    {
        // Arrange & Act
        var key = CacheKeyBuilder.Build(typeof(TestService), "GetProduct", null, 123);

        // Assert
        Assert.Equal("TestService.GetProduct(null, 123)", key);
    }

    [Fact]
    public void CacheKeyBuilder_WithComplexObject_ShouldSerializeToJson()
    {
        // Arrange
        var obj = new { Id = 1, Name = "Test" };

        // Act
        var key = CacheKeyBuilder.Build(typeof(TestService), "GetProduct", obj);

        // Assert
        Assert.Contains("TestService.GetProduct", key);
        Assert.Contains("\"Id\":1", key);
        Assert.Contains("\"Name\":\"Test\"", key);
    }

    [Fact]
    public void CommandorMemoryCache_SetAndGet_ShouldWork()
    {
        // Arrange
        var cache = new CommandorMemoryCache();
        var key = "test-key";
        var value = "test-value";

        // Act
        cache.Set(key, value);
        var result = cache.Get<string>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void CommandorMemoryCache_GetNonExistent_ShouldReturnDefault()
    {
        // Arrange
        var cache = new CommandorMemoryCache();

        // Act
        var result = cache.Get<string>("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CommandorMemoryCache_Remove_ShouldRemoveItem()
    {
        // Arrange
        var cache = new CommandorMemoryCache();
        var key = "test-key";
        cache.Set(key, "test-value");

        // Act
        cache.Remove(key);
        var result = cache.Get<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CommandorMemoryCache_Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var cache = new CommandorMemoryCache();
        cache.Set("key1", "value1");
        cache.Set("key2", "value2");
        cache.Set("key3", "value3");

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.Count);
        Assert.Null(cache.Get<string>("key1"));
        Assert.Null(cache.Get<string>("key2"));
        Assert.Null(cache.Get<string>("key3"));
    }

    [Fact]
    public void Computed_Create_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var computed = new Computed<string>("test-key", "test-value");

        // Assert
        Assert.True(computed.Version > 0);
        Assert.Equal("test-key", computed.CacheKey);
        Assert.Equal("test-value", computed.Value);
        Assert.True(computed.HasValue);
        Assert.False(computed.HasError);
        Assert.True(computed.IsConsistent());
    }

    [Fact]
    public void Computed_CreateWithError_ShouldStoreError()
    {
        // Arrange
        var error = new InvalidOperationException("Test error");

        // Act
        var computed = new Computed<string>("test-key", error);

        // Assert
        Assert.Null(computed.Value);
        Assert.Equal(error, computed.Error);
        Assert.False(computed.HasValue);
        Assert.True(computed.HasError);
    }

    [Fact]
    public void Computed_Invalidate_ShouldChangeState()
    {
        // Arrange
        var computed = new Computed<string>("test-key", "test-value");
        var invalidated = false;
        computed.Invalidated += _ => invalidated = true;

        // Act
        computed.Invalidate();

        // Assert
        Assert.False(computed.IsConsistent());
        Assert.Equal(ConsistencyState.Invalidated, computed.ConsistencyState);
        Assert.True(invalidated);
    }

    [Fact]
    public async Task Computed_WhenInvalidated_ShouldCompleteOnInvalidation()
    {
        // Arrange
        var computed = new Computed<string>("test-key", "test-value");
        var task = computed.WhenInvalidated();

        // Act
        computed.Invalidate();
        await task;

        // Assert
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void ComputedRegistry_RegisterAndGet_ShouldWork()
    {
        // Arrange
        var computed = new Computed<string>("registry-test", "value");
        ComputedRegistry.Clear(); // Clean state

        // Act
        ComputedRegistry.Register(computed);
        var retrieved = ComputedRegistry.Get<string>("registry-test");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(computed.CacheKey, retrieved.CacheKey);
        Assert.Equal(computed.Value, retrieved.Value);
        
        // Cleanup
        ComputedRegistry.Clear();
    }

    [Fact]
    public void ComputedRegistry_Remove_ShouldRemoveComputed()
    {
        // Arrange
        var computed = new Computed<string>("remove-test", "value");
        ComputedRegistry.Clear();
        ComputedRegistry.Register(computed);

        // Act
        ComputedRegistry.Remove("remove-test");
        var retrieved = ComputedRegistry.Get<string>("remove-test");

        // Assert
        Assert.Null(retrieved);
        
        // Cleanup
        ComputedRegistry.Clear();
    }

    [Fact]
    public void LiteApiComputedCache_SetAndGet_ShouldWork()
    {
        // Arrange
        var cache = new LiteApiComputedCache();
        var key = "liteapi-test-key";
        var value = new TestProduct { Id = 1, Name = "Test Product", Price = 1000 };

        // Act
        cache.Set(key, value);
        var result = cache.Get<TestProduct>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.Equal(value.Price, result.Price);
        
        // Cleanup
        cache.Remove(key);
    }

    // Helper classes
    private class TestService { }
    
    private class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
