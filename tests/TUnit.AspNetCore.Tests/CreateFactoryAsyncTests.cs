using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Extensions;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Coverage for <see cref="PerTestWebApplicationTest{TFactory,TEntryPoint}"/>: no application is built
/// before the test, and <c>CreateFactoryAsync</c> layers test-specific configuration over the
/// class-level overrides.
/// </summary>
public class PerTestWebApplicationTestTests : PerTestWebApplicationTest<TestWebAppFactory, Program>
{
    private bool _setupRan;
    private int _configureTestServicesCalls;

    protected override Task SetupAsync()
    {
        _setupRan = true;
        return Task.CompletedTask;
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        _configureTestServicesCalls++;
        services.AddSingleton<ClassMarker>();
        services.AddSingleton<IGreeting>(new Greeting("class"));
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Source"] = "class",
            ["ClassOnly"] = "present",
        });
    }

    [Test]
    public async Task No_Application_Is_Built_Before_The_Test()
    {
        await Assert.That(_setupRan).IsTrue();
        await Assert.That(_configureTestServicesCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Test_Services_Are_Layered_Over_Class_Services()
    {
        var factory = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.ReplaceService<IGreeting>(new Greeting("test"))));

        await Assert.That(factory.Services.GetService<ClassMarker>()).IsNotNull();
        await Assert.That(factory.Services.GetRequiredService<IGreeting>().Value).IsEqualTo("test");
    }

    [Test]
    public async Task Test_Configuration_Is_Layered_Over_Class_Configuration()
    {
        var factory = await CreateFactoryAsync(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Source"] = "test",
            })));

        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        await Assert.That(configuration["Source"]).IsEqualTo("test");
        await Assert.That(configuration["ClassOnly"]).IsEqualTo("present");
    }

    [Test]
    public async Task Created_Factory_Keeps_TUnit_Wiring()
    {
        var factory = await CreateFactoryAsync();

        await Assert.That(factory.Services.GetRequiredService<TestContext>()).IsSameReferenceAs(TestContext.Current);

        using var client = factory.CreateClient();
        var body = await client.GetStringAsync("/echo-headers");

        await Assert.That(body).Contains("X-TUnit-TestId");
    }

    [Test]
    public async Task Each_Call_Creates_A_Separate_Application()
    {
        var first = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.ReplaceService<IGreeting>(new Greeting("first"))));
        var second = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.ReplaceService<IGreeting>(new Greeting("second"))));

        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(first.Services.GetRequiredService<IGreeting>().Value).IsEqualTo("first");
        await Assert.That(second.Services.GetRequiredService<IGreeting>().Value).IsEqualTo("second");
    }

    [Test]
    public async Task DisposeFactoryAsync_Disposes_Every_Created_Factory()
    {
        var first = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<DisposalProbe>()));
        var second = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<DisposalProbe>()));

        var firstProbe = first.Services.GetRequiredService<DisposalProbe>();
        var secondProbe = second.Services.GetRequiredService<DisposalProbe>();

        // Invoke the After(Test) hook directly; it runs again after the test and must tolerate that.
        await DisposeFactoryAsync();

        await Assert.That(firstProbe.IsDisposed).IsTrue();
        await Assert.That(secondProbe.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Concurrent_Calls_Track_Every_Factory()
    {
        var factories = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<DisposalProbe>()))));

        var probes = factories.Select(factory => factory.Services.GetRequiredService<DisposalProbe>()).ToArray();

        await DisposeFactoryAsync();

        await Assert.That(probes).All(probe => probe.IsDisposed);
    }
}

/// <summary>
/// A failure while disposing one created application must not leave the others running.
/// </summary>
/// <remarks>
/// Uses its own factory type, so it gets its own session-shared global factory. That factory disposes
/// its derived factories again at session end; the failing hosted service only fails its first stop, and
/// <see cref="TestWebApplicationFactory{TEntryPoint}.SuppressHostedServiceExecutionContextFlow"/> is off
/// because its wrapper replays the first stop result, which would fail that second disposal too.
/// </remarks>
public class PerTestWebApplicationTestDisposalFailureTests : PerTestWebApplicationTest<StopFailureWebAppFactory, Program>
{
    [Test]
    public async Task DisposeFactoryAsync_Disposes_Remaining_Factories_When_One_Throws()
    {
        var failing = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.AddHostedService<ThrowingOnStopService>()));
        var healthy = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<DisposalProbe>()));

        var probe = healthy.Services.GetRequiredService<DisposalProbe>();

        await Assert.That(DisposeFactoryAsync).Throws<Exception>().WithMessageContaining(ThrowingOnStopService.Message);
        await Assert.That(probe.IsDisposed).IsTrue();
    }
}

public class StopFailureWebAppFactory : TestWebApplicationFactory<Program>
{
    protected override bool SuppressHostedServiceExecutionContextFlow => false;
}

/// <summary>
/// <see cref="WebApplicationTest{TFactory,TEntryPoint}"/> builds its default application before the test;
/// <c>CreateFactoryAsync</c> builds an additional application and leaves
/// <see cref="WebApplicationTest{TFactory,TEntryPoint}.Factory"/> untouched.
/// </summary>
public class WebApplicationTestCreateFactoryAsyncTests : WebApplicationTest<TestWebAppFactory, Program>
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IGreeting>(new Greeting("class"));
        services.AddSingleton<DisposalProbe>();
    }

    [Test]
    public async Task CreateFactoryAsync_Does_Not_Replace_Factory()
    {
        var defaultFactory = Factory;

        var factory = await CreateFactoryAsync(builder =>
            builder.ConfigureTestServices(services => services.ReplaceService<IGreeting>(new Greeting("test"))));

        await Assert.That(factory).IsNotSameReferenceAs(defaultFactory);
        await Assert.That(Factory).IsSameReferenceAs(defaultFactory);
        await Assert.That(Services.GetRequiredService<IGreeting>().Value).IsEqualTo("class");
        await Assert.That(factory.Services.GetRequiredService<IGreeting>().Value).IsEqualTo("test");
    }

    [Test]
    public async Task DisposeFactoryAsync_Disposes_Default_And_Created_Factories()
    {
        var defaultProbe = Services.GetRequiredService<DisposalProbe>();
        var factory = await CreateFactoryAsync();
        var createdProbe = factory.Services.GetRequiredService<DisposalProbe>();

        await DisposeFactoryAsync();

        await Assert.That(defaultProbe.IsDisposed).IsTrue();
        await Assert.That(createdProbe.IsDisposed).IsTrue();
    }
}

public interface IGreeting
{
    string Value { get; }
}

public sealed record Greeting(string Value) : IGreeting;

public sealed class ClassMarker;

public sealed class DisposalProbe : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

/// <summary>
/// Fails the first stop only, so the global factory's second disposal at session end succeeds.
/// </summary>
public sealed class ThrowingOnStopService : Microsoft.Extensions.Hosting.IHostedService
{
    public const string Message = "Stop failed on purpose.";

    private int _stopCalls;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) =>
        Interlocked.Increment(ref _stopCalls) == 1
            ? throw new InvalidOperationException(Message)
            : Task.CompletedTask;
}
