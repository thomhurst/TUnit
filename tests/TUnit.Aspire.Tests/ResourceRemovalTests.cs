using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using TUnit.Aspire.Tests.Helpers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Regression tests for #7284: removing a resource must also detach the WaitFor and WithReference
/// edges that kept resources hold on it. Uses a real builder so the edges are the ones Aspire's own
/// extension methods create, but never builds or starts the app.
/// </summary>
public class ResourceRemovalTests
{
    [Test]
    public async Task Remove_DropsWaitEdgesOnRemovedResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var optional = builder.AddContainer("optional", "nginx");
        var other = builder.AddContainer("other", "nginx");
        var api = builder.AddContainer("api", "nginx").WaitFor(optional).WaitFor(other);

        ResourceRemoval.Remove(builder.Resources, ["optional"], _ => { });

        var waits = api.Resource.Annotations.OfType<WaitAnnotation>().Select(w => w.Resource.Name).ToList();
        await Assert.That(waits).IsEquivalentTo(["other"]);
        await Assert.That(builder.Resources.Select(r => r.Name)).DoesNotContain("optional");
    }

    [Test]
    public async Task Remove_DropsEndpointReferenceEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var optional = builder.AddContainer("optional", "nginx").WithHttpEndpoint(targetPort: 80);
        var api = builder.AddContainer("api", "nginx")
            .WithReference(optional.GetEndpoint("http"))
            .WithEnvironment("OPTIONAL_URL", optional.GetEndpoint("http"))
            .WithEnvironment("KEEP", "value");

        ResourceRemoval.Remove(builder.Resources, ["optional"], _ => { });

        var environment = await RunEnvironmentCallbacksAsync(api.Resource);
        await Assert.That(environment.Keys).IsEquivalentTo(["KEEP"]);
        await Assert.That(api.Resource.Annotations.OfType<EndpointReferenceAnnotation>()).IsEmpty();
        await Assert.That(api.Resource.Annotations.OfType<ResourceRelationshipAnnotation>()
            .Where(r => r.Resource.Name == "optional")).IsEmpty();
    }

    [Test]
    public async Task Remove_DropsConnectionStringEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var cache = builder.AddConnectionString("cache");
        var api = builder.AddContainer("api", "nginx")
            .WithReference(cache)
            .WaitFor(cache)
            .WithEnvironment("KEEP", "value");

        ResourceRemoval.Remove(builder.Resources, ["cache"], _ => { });

        var environment = await RunEnvironmentCallbacksAsync(api.Resource);
        await Assert.That(environment.Keys).IsEquivalentTo(["KEEP"]);
        await Assert.That(api.Resource.Annotations.OfType<WaitAnnotation>()).IsEmpty();
    }

    [Test]
    public async Task Remove_KeepsReferencesToKeptResources()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("optional", "nginx");
        var cache = builder.AddConnectionString("cache");
        var api = builder.AddContainer("api", "nginx").WithReference(cache);

        ResourceRemoval.Remove(builder.Resources, ["optional"], _ => { });

        var environment = await RunEnvironmentCallbacksAsync(api.Resource);
        await Assert.That(environment.Keys).Contains("ConnectionStrings__cache");
    }

    [Test]
    public async Task Remove_DropsArgumentsThatReferenceRemovedResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var optional = builder.AddContainer("optional", "nginx").WithHttpEndpoint(targetPort: 80);
        var api = builder.AddContainer("api", "nginx")
            .WithArgs(context =>
            {
                context.Args.Add("--verbose");
                context.Args.Add(optional.GetEndpoint("http"));
            });

        ResourceRemoval.Remove(builder.Resources, ["optional"], _ => { });

        var args = new List<object>();
        var context = new CommandLineArgsCallbackContext(args, api.Resource);
        foreach (var annotation in api.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        await Assert.That(args).IsEquivalentTo(new object[] { "--verbose" });
    }

    [Test]
    public async Task Remove_AlsoRemovesChildrenOfRemovedResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var server = builder.AddContainer("server", "postgres");
        var database = builder.AddResource(new FakeChildComputeResource("database", server.Resource));
        builder.AddResource(new FakeChildComputeResource("schema", database.Resource));
        var api = builder.AddContainer("api", "nginx").WaitFor(database);
        var log = new List<string>();

        ResourceRemoval.Remove(builder.Resources, ["server"], log.Add);

        await Assert.That(builder.Resources.Select(r => r.Name)).IsEquivalentTo(["api"]);
        await Assert.That(api.Resource.Annotations.OfType<WaitAnnotation>()).IsEmpty();
        await Assert.That(log).Contains("ResourcesToRemove: also removing 'database' because its parent 'server' is removed.");
    }

    [Test]
    public async Task Remove_LogsUnknownResourceAndChangesNothing()
    {
        var builder = DistributedApplication.CreateBuilder();
        var optional = builder.AddContainer("optional", "nginx");
        var api = builder.AddContainer("api", "nginx").WaitFor(optional);
        var log = new List<string>();

        ResourceRemoval.Remove(builder.Resources, ["missing"], log.Add);

        await Assert.That(log).IsEquivalentTo(["ResourcesToRemove: resource 'missing' not found (skipped)."]);
        await Assert.That(api.Resource.Annotations.OfType<WaitAnnotation>()).Count().IsEqualTo(1);
    }

    private static async Task<Dictionary<string, object>> RunEnvironmentCallbacksAsync(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), resource);

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return context.EnvironmentVariables;
    }
}
