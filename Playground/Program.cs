using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core;
using TUnit.Engine.Extensions;
using Xunit;

namespace Playground;

public static class Program
{
#pragma warning disable TUnit0034
    public static async Task<int> Main(string[] args)
#pragma warning restore TUnit0034
    {
        var builder = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);

        builder.AddTUnit();

        using var app = await builder.BuildAsync();

        return await app.RunAsync();
    }
}

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

    [Before(HookType.Assembly)]
    public static async Task Before()
    {
        await PostgreSqlContainer.StartAsync();
        await RedisContainer.StartAsync();
    }

    [After(HookType.Assembly)]
    public static async Task After()
    {
        await PostgreSqlContainer.StopAsync();
        await PostgreSqlContainer.DisposeAsync();
        await RedisContainer.StopAsync();
        await RedisContainer.DisposeAsync();
    }
}
