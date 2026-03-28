using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Reproduction and fix verification tests for https://github.com/thomhurst/TUnit/issues/5260
/// </summary>
public class WaitForHealthyReproductionTests
{
    /// <summary>
    /// Confirms the Aspire 13.2.0 breaking change: ParameterResource no longer
    /// implements IResourceWithoutLifetime.
    /// </summary>
    [Test]
    [Category("Docker")]
    public async Task ParameterResource_DoesNotImplement_IResourceWithoutLifetime_InAspire13_2(CancellationToken ct)
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TUnit_Aspire_Tests_AppHost>();
        await using var app = await builder.BuildAsync();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var paramResource = model.Resources.First(r => r.Name == "my-secret");

        await Assert.That(paramResource is IResourceWithoutLifetime).IsFalse();
    }

    /// <summary>
    /// Confirms that ParameterResource is NOT an IComputeResource,
    /// so our fix correctly excludes it from the waitable list.
    /// </summary>
    [Test]
    [Category("Docker")]
    public async Task ParameterResource_IsNot_IComputeResource(CancellationToken ct)
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TUnit_Aspire_Tests_AppHost>();
        await using var app = await builder.BuildAsync();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var paramResource = model.Resources.First(r => r.Name == "my-secret");
        var containerResource = model.Resources.First(r => r.Name == "nginx-no-healthcheck");

        await Assert.That(paramResource is IComputeResource).IsFalse();
        await Assert.That(containerResource is IComputeResource).IsTrue();
    }

    /// <summary>
    /// Confirms WaitForResourceHealthyAsync hangs on parameter resources.
    /// </summary>
    [Test]
    [Category("Docker")]
    public async Task WaitForResourceHealthyAsync_OnParameterResource_Hangs(CancellationToken ct)
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TUnit_Aspire_Tests_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync(ct);

        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        var timedOut = false;
        try
        {
            await notificationService.WaitForResourceHealthyAsync("my-secret", cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
        }
        catch (InvalidOperationException)
        {
            timedOut = true;
        }

        await Assert.That(timedOut).IsTrue();
    }

    /// <summary>
    /// With the IComputeResource fix, AspireFixture AllHealthy should succeed
    /// even when the AppHost has parameter resources.
    /// </summary>
    [Test]
    [Category("Docker")]
    public async Task AspireFixture_AllHealthy_Succeeds_AfterFix(CancellationToken ct)
    {
        var fixture = new TestFixtureAllHealthy();

        try
        {
            await fixture.InitializeAsync();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}

public class TestFixtureAllHealthy : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(60);
}
