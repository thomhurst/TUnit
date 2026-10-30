using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Fixture that removes the <c>nginx-no-healthcheck</c> resource before the app starts, plus an
/// optional dependency that the kept <c>api-service</c> waits on and references (#7284).
/// </summary>
public class FixtureWithRemovedResource : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(120);
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;
    protected override bool EnableTelemetryCollection => false;

    protected override IEnumerable<string> ResourcesToWaitFor() => ["api-service"];

    protected override IEnumerable<string> ResourcesToRemove() => ["nginx-no-healthcheck", "optional-service", "optional-db"];

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        // Without edge cleanup, api-service waits forever on the removed resources, or fails to
        // resolve their endpoint and connection string.
        var optionalService = builder.AddContainer("optional-service", "nginx").WithHttpEndpoint(targetPort: 80);
        var optionalDb = builder.AddConnectionString("optional-db");

        builder.CreateResourceBuilder<ProjectResource>("api-service")
            .WithReference(optionalService.GetEndpoint("http"))
            .WithReference(optionalDb)
            .WaitFor(optionalService)
            .WaitFor(optionalDb);
    }
}

/// <summary>
/// Verifies that resources listed in <see cref="AspireFixture{TAppHost}.ResourcesToRemove"/>
/// are removed from the distributed application before it starts.
/// </summary>
[ClassDataSource<FixtureWithRemovedResource>(Shared = SharedType.PerTestSession)]
[Category("Docker")]
[Category("Integration")]
public class ResourcesToRemoveTests(FixtureWithRemovedResource fixture)
{
    [Test]
    public async Task RemovedResource_IsNotPresentInAppModel()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNames = model.Resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).DoesNotContain("nginx-no-healthcheck");
        await Assert.That(resourceNames).DoesNotContain("optional-service");
        await Assert.That(resourceNames).DoesNotContain("optional-db");
    }

    [Test]
    public async Task KeptResource_HasNoWaitOnRemovedResources()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var api = model.Resources.Single(r => r.Name == "api-service");

        await Assert.That(api.Annotations.OfType<WaitAnnotation>()).IsEmpty();
    }

    [Test]
    public async Task OtherResources_AreStillPresent()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNames = model.Resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).Contains("api-service");
    }
}
