using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Tests for <see cref="TestWebApplicationFactory{TEntryPoint}"/>'s automatic
/// <see cref="ExecutionContext.SuppressFlow"/> wrapping of <see cref="IHostedService"/>
/// registrations. See issue #5589.
/// </summary>
public class HostedServiceFlowSuppressionTests
{
    [Test]
    public async Task StartAsync_SuppressesFlow_IntoSpawnedTasks()
    {
        using var outer = new Activity("outer-test").Start();

        var probe = new FlowProbeHostedService();
        await using var factory = new FlowSuppressTestFactory { Probe = probe };

        _ = factory.Server;

        await probe.SpawnedTaskCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(probe.ActivityInSpawnedTask).IsNull();
    }

    [Test]
    public async Task OptOut_PreservesFlow_IntoSpawnedTasks()
    {
        using var outer = new Activity("outer-test").Start();

        var probe = new FlowProbeHostedService();
        await using var factory = new NoSuppressionFactory { Probe = probe };

        _ = factory.Server;

        await probe.SpawnedTaskCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(probe.ActivityInSpawnedTask).IsEqualTo(outer);
    }

    [Test]
    public async Task RegisteredHostedServices_AreWrapped()
    {
        var probe = new FlowProbeHostedService();
        await using var factory = new FlowSuppressTestFactory { Probe = probe };

        _ = factory.Server;

        var hostedServices = factory.Services.GetServices<IHostedService>().ToList();

        await Assert.That(hostedServices.Any(h => h is FlowSuppressingHostedService)).IsTrue();
    }

    [Test]
    public async Task IsolatedFactory_HostedServices_AreWrapped()
    {
        await using var factory = new FlowSuppressTestFactory();

        var isolated = factory.GetIsolatedFactory(
            TestContext.Current!,
            new WebApplicationTestOptions(),
            services => services.AddSingleton<IHostedService>(new FlowProbeHostedService()),
            (_, _) => { });

        _ = isolated.Server;

        var hostedServices = isolated.Services.GetServices<IHostedService>().ToList();

        await Assert.That(hostedServices.Any(h => h is FlowSuppressingHostedService)).IsTrue();
    }
}

internal class FlowSuppressTestFactory : TestWebApplicationFactory<Program>
{
    public FlowProbeHostedService? Probe { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            if (Probe is not null)
            {
                services.AddSingleton<IHostedService>(Probe);
            }
        });
    }
}

internal sealed class NoSuppressionFactory : FlowSuppressTestFactory
{
    protected override bool SuppressHostedServiceExecutionContextFlow => false;
}

internal sealed class FlowProbeHostedService : IHostedService
{
    public Activity? ActivityInSpawnedTask { get; private set; }
    public TaskCompletionSource SpawnedTaskCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() =>
        {
            ActivityInSpawnedTask = Activity.Current;
            SpawnedTaskCompleted.TrySetResult();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
