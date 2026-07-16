using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4049;

/// <summary>
/// Regression test for issue #4049: Property getters should not be called during discovery
/// for properties that don't return IAsyncInitializer.
///
/// This simulates the WebApplicationFactory.Server scenario where accessing a property
/// causes side effects (like starting a test server) before the object is configured.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class PropertyGetterSideEffectTests
{
    private static int _sideEffectCount = 0;

    /// <summary>
    /// Simulates a class like WebApplicationFactory where accessing certain properties
    /// triggers initialization (like building the test host).
    /// </summary>
    public class FixtureWithSideEffects : IAsyncInitializer
    {
        private object? _server;

        /// <summary>
        /// Simulates WebApplicationFactory.Server - accessing this property has side effects.
        /// This property type is 'object', NOT IAsyncInitializer, so TUnit should NOT access it.
        /// </summary>
        public object Server
        {
            get
            {
                Interlocked.Increment(ref _sideEffectCount);
                Console.WriteLine($"Server getter called! Side effect count: {_sideEffectCount}");
                _server ??= new object(); // Side effect: lazy initialization
                return _server;
            }
        }

        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            IsInitialized = true;
            Console.WriteLine("FixtureWithSideEffects.InitializeAsync called");
            return Task.CompletedTask;
        }
    }

    [Test]
    [ClassDataSource<FixtureWithSideEffects>]
    public async Task PropertyGetters_WithSideEffects_ShouldNotBeCalledDuringDiscovery(FixtureWithSideEffects fixture)
    {
        // Verify the fixture was properly initialized
        await Assert.That(fixture.IsInitialized).IsTrue();

        // The Server property getter should NOT have been called during discovery.
        // It should only be called if the test explicitly accesses it.
        // At this point, we haven't accessed Server, so count should be 0.
        await Assert.That(_sideEffectCount).IsEqualTo(0)
            .Because("TUnit should not access non-IAsyncInitializer property getters during discovery");

        // Now let's explicitly access the Server to verify it works
        var server = fixture.Server;
        await Assert.That(server).IsNotNull();
        await Assert.That(_sideEffectCount).IsEqualTo(1);

        Console.WriteLine($"Test passed: Server getter was only called when explicitly accessed (count: {_sideEffectCount})");
    }
}

/// <summary>
/// Test to verify that nested IAsyncInitializer properties ARE still discovered
/// when they are properly typed.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class NestedAsyncInitializerDiscoveryTests
{
    private static int _parentInitCount = 0;
    private static int _childInitCount = 0;

    public class ChildInitializer : IAsyncInitializer
    {
        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _childInitCount);
            IsInitialized = true;
            Console.WriteLine($"ChildInitializer.InitializeAsync called (count: {_childInitCount})");
            return Task.CompletedTask;
        }
    }

    public class ParentWithNestedInitializer : IAsyncInitializer
    {
        /// <summary>
        /// This property IS typed as IAsyncInitializer, so TUnit SHOULD access it
        /// and discover the nested initializer for proper lifecycle management.
        /// </summary>
        public IAsyncInitializer NestedInitializer { get; } = new ChildInitializer();

        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _parentInitCount);
            IsInitialized = true;
            Console.WriteLine($"ParentWithNestedInitializer.InitializeAsync called (count: {_parentInitCount})");
            return Task.CompletedTask;
        }
    }

    [Test]
    [ClassDataSource<ParentWithNestedInitializer>]
    public async Task NestedInitializers_WhenProperlyTyped_ShouldBeDiscoveredAndInitialized(ParentWithNestedInitializer parent)
    {
        // Verify parent was initialized
        await Assert.That(parent.IsInitialized).IsTrue();
        await Assert.That(_parentInitCount).IsGreaterThan(0);

        // Verify child was discovered and initialized
        var child = parent.NestedInitializer as ChildInitializer;
        await Assert.That(child).IsNotNull();
        await Assert.That(child!.IsInitialized).IsTrue()
            .Because("TUnit should discover and initialize properly-typed IAsyncInitializer properties");
        await Assert.That(_childInitCount).IsGreaterThan(0);

        Console.WriteLine($"Test passed: Both parent ({_parentInitCount}) and child ({_childInitCount}) were initialized");
    }
}
