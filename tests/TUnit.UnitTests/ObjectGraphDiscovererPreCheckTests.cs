using System.Reflection;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

/// <summary>
/// <see cref="ObjectGraphDiscoverer.MayHaveNestedObjects"/> is a memoized pre-check that lets callers skip
/// graph construction. It must never return <c>false</c> for a type whose full traversal would find
/// something. These tests compare the gated path against the ungated core traversal for edge cases
/// where a property's declared type differs from the runtime value's type.
/// </summary>
public class ObjectGraphDiscovererPreCheckTests
{
    [Test]
    public async Task PlainClass_HasNoNestedObjects()
    {
        await AssertParity(new PlainHolder(), expectedMayHaveNested: false, expectedNested: []);
    }

    [Test]
    public async Task ObjectTypedProperty_WithInitializerRuntimeValue_IsNotTraversed()
    {
        // Declared type 'object' is not IAsyncInitializer-assignable, so neither the source-gen registry
        // nor the reflection fallback reads the getter (avoids getter side effects, see #4049).
        await AssertParity(new ObjectTypedHolder(), expectedMayHaveNested: false, expectedNested: []);
    }

    [Test]
    public async Task NonInitializerInterfaceProperty_WithInitializerRuntimeValue_IsNotTraversed()
    {
        await AssertParity(new MarkerInterfaceHolder(), expectedMayHaveNested: false, expectedNested: []);
    }

    [Test]
    public async Task NonInitializerBaseClassProperty_WithInitializerRuntimeValue_IsNotTraversed()
    {
        await AssertParity(new NonInitializerBaseHolder(), expectedMayHaveNested: false, expectedNested: []);
    }

    [Test]
    public async Task InitializerInterfaceProperty_IsTraversed()
    {
        var holder = new InitializerInterfaceHolder();
        await AssertParity(holder, expectedMayHaveNested: true, expectedNested: [holder.Value]);
    }

    [Test]
    public async Task InheritedInitializerProperty_IsTraversed()
    {
        var holder = new DerivedHolder();
        await AssertParity(holder, expectedMayHaveNested: true, expectedNested: [holder.Value]);
    }

    [Test]
    public async Task StaticInitializerProperty_IsNotTraversedAsNested()
    {
        // Static properties are handled by StaticPropertyHandler, not instance graph traversal.
        await AssertParity(new StaticHolder(), expectedMayHaveNested: false, expectedNested: []);
    }

    [Test]
    public async Task InitializerType_WithSourceGenRegisteredProperties_IsTraversed()
    {
        // The holder itself implements IAsyncInitializer, so in source-gen mode its initializer
        // properties come from InitializerPropertyRegistry (the flattened branch of the pre-check).
        var holder = new InitializerHolderWithMixedProperties();
        await AssertParity(holder, expectedMayHaveNested: true, expectedNested: [holder.Typed]);
    }

    [Test]
    public async Task DataSourceInjectedProperty_DeclaredAsNonInitializerInterface_IsTraversed()
    {
        // Injectable (data-source) properties are traversed regardless of declared type, so an
        // IAsyncInitializer runtime value behind a plain interface is still discovered.
        var holder = new InjectedMarkerInterfaceHolder { Value = new MarkerInitializer() };
        await AssertParity(holder, expectedMayHaveNested: true, expectedNested: [holder.Value]);
    }

    private static async Task AssertParity(object root, bool expectedMayHaveNested, object[] expectedNested)
    {
        var mayHaveNested = ObjectGraphDiscoverer.MayHaveNestedObjects(root.GetType());
        var gated = NestedObjects(new ObjectGraphDiscoverer().DiscoverNestedObjectGraph(root));
        var ungated = DiscoverUngated(root);

        await Assert.That(mayHaveNested).IsEqualTo(expectedMayHaveNested);
        await Assert.That(gated).IsEquivalentTo(ungated);
        await Assert.That(gated).IsEquivalentTo(expectedNested);

        if (!mayHaveNested)
        {
            await Assert.That(ungated).IsEmpty();
        }
    }

    private static List<object> NestedObjects(ObjectGraph graph)
    {
        var result = new List<object>();
        foreach (var depth in graph.GetDepthsDescending())
        {
            if (depth > 0)
            {
                result.AddRange(graph.GetObjectsAtDepth(depth));
            }
        }

        return result;
    }

    /// <summary>
    /// Runs the core traversal for the root without the <see cref="ObjectGraphDiscoverer.MayHaveNestedObjects"/> gate.
    /// </summary>
    private static List<object> DiscoverUngated(object root)
    {
        var core = typeof(ObjectGraphDiscoverer).GetMethod("DiscoverNestedObjectsCore", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DiscoverNestedObjectsCore not found");

        var objectsByDepth = new Dictionary<int, HashSet<object>>();
        var visited = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance) { root };
        core.Invoke(new ObjectGraphDiscoverer(), [root, objectsByDepth, visited, 1, CancellationToken.None]);

        return objectsByDepth.Where(kvp => kvp.Key > 0).SelectMany(kvp => kvp.Value).ToList();
    }

    public interface IMarker;

    public interface IInitializerMarker : IAsyncInitializer;

    public class NonInitializerBase;

    public class MarkerInitializer : NonInitializerBase, IMarker, IInitializerMarker
    {
        public Task InitializeAsync() => Task.CompletedTask;
    }

    public class PlainHolder
    {
        public string Name { get; } = "plain";
        public int Number { get; } = 1;
    }

    public class ObjectTypedHolder
    {
        public object Value { get; } = new MarkerInitializer();
    }

    public class MarkerInterfaceHolder
    {
        public IMarker Value { get; } = new MarkerInitializer();
    }

    public class NonInitializerBaseHolder
    {
        public NonInitializerBase Value { get; } = new MarkerInitializer();
    }

    public class InitializerInterfaceHolder
    {
        public IInitializerMarker Value { get; } = new MarkerInitializer();
    }

    public class InitializerTypedBase
    {
        public MarkerInitializer Value { get; } = new();
    }

    public class DerivedHolder : InitializerTypedBase;

    public class StaticHolder
    {
        public static MarkerInitializer Shared { get; } = new();
    }

    public class InitializerHolderWithMixedProperties : IAsyncInitializer
    {
        public MarkerInitializer Typed { get; } = new();
        public object Untyped { get; } = new MarkerInitializer();

        public Task InitializeAsync() => Task.CompletedTask;
    }

    public class InjectedMarkerInterfaceHolder
    {
        [ClassDataSource<MarkerInitializer>]
        public required IMarker Value { get; init; }
    }
}
