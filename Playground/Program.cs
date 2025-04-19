using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Playground;

public class Tests
{
    [Fact]
    public void Test()
    {
        var one = "1";
        
        Xunit.Assert.Equal("1", one);
    }
}

public class Hooks
{
    public static PostgreSqlContainer PostgreSqlContainer { get; } = new PostgreSqlBuilder().Build();
    public static RedisContainer RedisContainer { get; } = new RedisBuilder().Build();
        
    [Before(Assembly)]
    public static async Task Before()
    {
        await PostgreSqlContainer.StartAsync();
        await RedisContainer.StartAsync();
    }
    
    [After(Assembly)]
    public static async Task After()
    {
        await PostgreSqlContainer.StopAsync();
        await PostgreSqlContainer.DisposeAsync();
        await RedisContainer.StopAsync();
        await RedisContainer.DisposeAsync();
    }
}