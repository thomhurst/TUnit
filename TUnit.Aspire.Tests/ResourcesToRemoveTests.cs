using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;
using Aspire.Hosting.ApplicationModel;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Fixture that removes the <c>nginx-no-healthcheck</c> resource before the app starts.
/// </summary>
public class FixtureWithRemovedResource : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(120);
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.None;
    protected override bool EnableTelemetryCollection => false;

    protected override IEnumerable<string> ResourcesToRemove() => ["nginx-no-healthcheck"];
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
    }

    [Test]
    public async Task OtherResources_AreStillPresent()
    {
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNames = model.Resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).Contains("api-service");
    }
}
