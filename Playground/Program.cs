using Testcontainers.PostgreSql;
using Testcontainers.Redis;

public class Tests
{
    [Test, Repeat(1000)]
    public void Test()
    {
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