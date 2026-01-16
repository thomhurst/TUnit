using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TUnit.Example.Asp.Net.TestProject.Bugs._4431;

public class InMemoryPostgres : IAsyncInitializer, IAsyncDisposable
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();
    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

// Factory with shared container
public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryPostgres>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgres Postgres { get; init; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", Postgres.Container.GetConnectionString() }
            });
        });
    }
}

// Base class
public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureTestConfiguration(config);
    }
}

// Actual tests
public class WeatherTests : TestsBase
{
    [Test]
    public async Task GetWeatherForecast_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}

public class ParentTest
{

    [ClassDataSource<InMemoryPostgres>(Shared = SharedType.PerTestSession)]
    public InMemoryPostgres Postgres { get; init; } = null!;

    [Test]
    public async Task Should_Succeed()
    {
        await Assert.That(Postgres.Container.GetConnectionString()).IsNotNull();
    }
}


public class ChildTest<T> where T : ParentTest, new()
{
    [Test]
    public async Task Should_Succeed()
    {
        T testInstance = new T();
        await Assert.That(testInstance.Postgres.Container.GetConnectionString()).IsNotNull();
    }
}

public class SecondChildTest : ParentTest
{
    [Test]
    public async Task Should_Succeed()
    {
        await Assert.That(Postgres.Container.GetConnectionString()).IsNotNull();
    }
}
