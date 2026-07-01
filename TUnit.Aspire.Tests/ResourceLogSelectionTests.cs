using Aspire.Hosting.ApplicationModel;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Pure (no Docker) tests for <see cref="AspireFixture{TAppHost}.SelectResourceLogNames"/>, the
/// resource-selection logic behind opt-in log forwarding. Constructs the fixture without starting
/// it, so no application is built.
/// </summary>
public class ResourceLogSelectionTests
{
    private static readonly AspireFixture<Projects.TUnit_Aspire_Tests_AppHost> Fixture = new();

    [Test]
    public async Task ExplicitNames_ReturnsOnlyTheNamedSubset_Ordinal()
    {
        IReadOnlyList<IResource> resources =
            [new FakeCompute("api"), new FakeCompute("chat"), new FakeCompute("db"), new FakeCompute("cache")];

        var names = Fixture.SelectResourceLogNames(resources,
            new AspireFixtureOptions { ResourceLogNames = ["chat", "api", "missing"] });

        // Only resources that exist are returned; the unknown "missing" is dropped.
        await Assert.That(names).Contains("api");
        await Assert.That(names).Contains("chat");
        await Assert.That(names).DoesNotContain("db");
        await Assert.That(names).DoesNotContain("cache");
        await Assert.That(names).DoesNotContain("missing");
    }

    [Test]
    public async Task ExplicitNames_AreCaseSensitive()
    {
        IReadOnlyList<IResource> resources = [new FakeCompute("chat")];

        var names = Fixture.SelectResourceLogNames(resources,
            new AspireFixtureOptions { ResourceLogNames = ["Chat"] });

        await Assert.That(names).IsEmpty();
    }

    [Test]
    public async Task NoNames_FallsBackToWaitableResources()
    {
        var parent = new FakeCompute("api");
        IReadOnlyList<IResource> resources =
        [
            new FakeCompute("api"),          // compute, no parent  -> waited on
            new FakeResource("db-password"), // non-compute         -> excluded
            new FakeChildCompute("api-pdb", parent), // compute w/ parent -> excluded
        ];

        var names = Fixture.SelectResourceLogNames(resources, new AspireFixtureOptions());

        await Assert.That(names).Contains("api");
        await Assert.That(names).DoesNotContain("db-password");
        await Assert.That(names).DoesNotContain("api-pdb");
    }

    [Test]
    public async Task EmptyNames_IsTreatedAsNoNames()
    {
        IReadOnlyList<IResource> resources = [new FakeCompute("api"), new FakeResource("param")];

        var names = Fixture.SelectResourceLogNames(resources,
            new AspireFixtureOptions { ResourceLogNames = [] });

        await Assert.That(names).Contains("api");
        await Assert.That(names).DoesNotContain("param");
    }

    private sealed class FakeResource(string name) : IResource
    {
        public string Name { get; } = name;
        public ResourceAnnotationCollection Annotations { get; } = [];
    }

    private sealed class FakeCompute(string name) : IComputeResource
    {
        public string Name { get; } = name;
        public ResourceAnnotationCollection Annotations { get; } = [];
    }

    private sealed class FakeChildCompute(string name, IResource parent) : IComputeResource, IResourceWithParent
    {
        public string Name { get; } = name;
        public ResourceAnnotationCollection Annotations { get; } = [];
        public IResource Parent { get; } = parent;
    }
}
