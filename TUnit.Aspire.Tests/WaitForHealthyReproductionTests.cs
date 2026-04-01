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
            // Aspire throws InvalidOperationException for resources that can never become healthy
            timedOut = true;
        }

        await Assert.That(timedOut).IsTrue();
    }

    [Test]
    [Category("Docker")]
    public async Task AspireFixture_AllHealthy_Succeeds_AfterFix(CancellationToken ct)
    {
        var fixture = new HealthyFixture();

        try
        {
            await fixture.InitializeAsync();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    /// <summary>
    /// Regression test for https://github.com/thomhurst/TUnit/issues/5260 (Aspire 13.2.0+).
    ///
    /// Aspire 13.2.0 introduced ProjectRebuilderResource: an IComputeResource that also implements
    /// IResourceWithParent. Without the fix, ShouldWaitForResource returns true for it, causing
    /// WaitForResourceHealthyAsync to hang (it never emits healthy/running state).
    /// </summary>
    [Test]
    public async Task ShouldWaitForResource_IncludesComputeResource()
    {
        var fixture = new InspectableFixture();
        var regular = new FakeContainerResource("my-container");

        await Assert.That(fixture.TestShouldWaitForResource(regular)).IsTrue();
    }

    [Test]
    public async Task ShouldWaitForResource_ExcludesIResourceWithParent()
    {
        var fixture = new InspectableFixture();
        var regular = new FakeContainerResource("my-container");
        var rebuilder = new FakeRebuilderResource("my-container-rebuilder", regular);

        await Assert.That(fixture.TestShouldWaitForResource(rebuilder)).IsFalse();
    }

    [Test]
    public async Task ShouldWaitForResource_ExcludesNonComputeResource()
    {
        var fixture = new InspectableFixture();
        var paramResource = new FakeNonComputeResource("my-param");

        await Assert.That(fixture.TestShouldWaitForResource(paramResource)).IsFalse();
    }

    private sealed class HealthyFixture : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
    {
        protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Exposes <see cref="AspireFixture{TAppHost}.ShouldWaitForResource"/> for unit testing.
    /// </summary>
    private sealed class InspectableFixture : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
    {
        public bool TestShouldWaitForResource(IResource resource)
            => ShouldWaitForResource(resource);
    }

    /// <summary>A plain IComputeResource with no parent.</summary>
    private sealed class FakeContainerResource(string name) : IComputeResource
    {
        public string Name => name;
        public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
    }

    /// <summary>A non-compute resource (e.g. ParameterResource, ConnectionStringResource).</summary>
    private sealed class FakeNonComputeResource(string name) : IResource
    {
        public string Name => name;
        public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
    }

    /// <summary>
    /// Simulates ProjectRebuilderResource from Aspire 13.2.0:
    /// an IComputeResource that also implements IResourceWithParent.
    /// </summary>
    private sealed class FakeRebuilderResource(string name, IResource parent)
        : IComputeResource, IResourceWithParent
    {
        public string Name => name;
        public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
        public IResource Parent => parent;
    }
}
