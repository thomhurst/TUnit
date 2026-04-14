using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Reproduction and fix verification tests for https://github.com/thomhurst/TUnit/issues/5260
///
/// Docker-dependent tests share the session-wide <see cref="IntegrationTestFixture"/> so they
/// don't create competing Aspire applications. The remaining unit tests use in-memory fakes.
/// </summary>
[ClassDataSource<IntegrationTestFixture>(Shared = SharedType.PerTestSession)]
public class WaitForHealthyReproductionTests(IntegrationTestFixture fixture)
{
    [Test]
    [Category("Docker")]
    public async Task ParameterResource_DoesNotImplement_IResourceWithoutLifetime_InAspire13_2()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var paramResource = model.Resources.First(r => r.Name == "my-secret");

        await Assert.That(paramResource is IResourceWithoutLifetime).IsFalse();
    }

    [Test]
    [Category("Docker")]
    public async Task ParameterResource_IsNot_IComputeResource()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var paramResource = model.Resources.First(r => r.Name == "my-secret");
        var containerResource = model.Resources.First(r => r.Name == "nginx-no-healthcheck");

        await Assert.That(paramResource is IComputeResource).IsFalse();
        await Assert.That(containerResource is IComputeResource).IsTrue();
    }

    [Test]
    [Category("Docker")]
    public async Task WaitForResourceHealthyAsync_OnParameterResource_Hangs(CancellationToken ct)
    {
        var notificationService = fixture.App.Services.GetRequiredService<ResourceNotificationService>();

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

    /// <summary>
    /// Verifies the fix for #5260: <see cref="AspireFixture{TAppHost}.ShouldWaitForResource"/>
    /// correctly filters out parameter resources and <see cref="IResourceWithParent"/>, so
    /// <c>WaitForResourceHealthyAsync</c> completes on all waitable resources without hanging.
    /// Uses the shared app instead of creating a new fixture to avoid Docker contention on CI.
    /// </summary>
    [Test]
    [Category("Docker")]
    public async Task AllHealthy_Succeeds_OnWaitableResources(CancellationToken ct)
    {
        var notificationService = fixture.App.Services.GetRequiredService<ResourceNotificationService>();
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();

        // Apply the same filter that AspireFixture.ShouldWaitForResource uses
        var waitableResources = model.Resources
            .Where(r => r is IComputeResource && r is not IResourceWithParent)
            .ToList();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        foreach (var resource in waitableResources)
        {
            await notificationService.WaitForResourceHealthyAsync(resource.Name, cts.Token);
        }

        // If we reach here, AllHealthy would succeed — the fix is working
        await Assert.That(waitableResources).IsNotEmpty();
    }

    [Test]
    public async Task ShouldWaitForResource_IncludesComputeResource()
    {
        var inspectable = new InspectableFixture();
        var regular = new FakeContainerResource("my-container");

        await Assert.That(inspectable.TestShouldWaitForResource(regular)).IsTrue();
    }

    /// <summary>
    /// Regression test for https://github.com/thomhurst/TUnit/issues/5260 (Aspire 13.2.0+).
    ///
    /// Aspire 13.2.0 introduced ProjectRebuilderResource: an IComputeResource that also implements
    /// IResourceWithParent. Without the fix, ShouldWaitForResource returns true for it, causing
    /// WaitForResourceHealthyAsync to hang (it never emits healthy/running state).
    /// </summary>
    [Test]
    public async Task ShouldWaitForResource_ExcludesIResourceWithParent()
    {
        var inspectable = new InspectableFixture();
        var regular = new FakeContainerResource("my-container");
        var rebuilder = new FakeRebuilderResource("my-container-rebuilder", regular);

        await Assert.That(inspectable.TestShouldWaitForResource(rebuilder)).IsFalse();
    }

    [Test]
    public async Task ShouldWaitForResource_ExcludesNonComputeResource()
    {
        var inspectable = new InspectableFixture();
        var paramResource = new FakeNonComputeResource("my-param");

        await Assert.That(inspectable.TestShouldWaitForResource(paramResource)).IsFalse();
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
