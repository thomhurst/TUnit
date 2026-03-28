using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Verifies that IComputeResource is the right discriminator for lifecycle vs config resources.
/// </summary>
public class VerifyIComputeResource
{
    [Test]
    [Category("Docker")]
    public async Task ContainerResource_Is_IComputeResource(CancellationToken ct)
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TUnit_Aspire_Tests_AppHost>();
        await using var app = await builder.BuildAsync();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var nginx = model.Resources.First(r => r.Name == "nginx-no-healthcheck");
        var parameter = model.Resources.First(r => r.Name == "my-secret");

        // Container should be IComputeResource
        await Assert.That(nginx is IComputeResource).IsTrue();

        // Parameter should NOT be IComputeResource
        await Assert.That(parameter is IComputeResource).IsFalse();
    }
}
