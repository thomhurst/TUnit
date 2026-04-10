using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for <see cref="TestContextResolverRegistry"/> and <see cref="ITestContextResolver"/>.
/// Verifies that custom resolvers are consulted before the AsyncLocal chain when resolving Context.Current.
/// </summary>
[NotInParallel]
public class TestContextResolverRegistryTests
{
    [Before(Test)]
    public void SetUp()
    {
        TestContextResolverRegistry.Clear();
    }

    [After(Test)]
    public void TearDown()
    {
        TestContextResolverRegistry.Clear();
    }

    [Test]
    public async Task Resolve_NoResolversRegistered_ReturnsNull()
    {
        // Act
        var result = TestContextResolverRegistry.Resolve();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Resolve_ResolverReturnsNull_ReturnsNull()
    {
        // Arrange
        TestContextResolverRegistry.Register(new NullResolver());

        // Act
        var result = TestContextResolverRegistry.Resolve();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Resolve_ResolverReturnsContext_ReturnsIt()
    {
        // Arrange
        var expectedContext = TestContext.Current!;
        TestContextResolverRegistry.Register(new FixedResolver(expectedContext));

        // Act
        var result = TestContextResolverRegistry.Resolve();

        // Assert
        await Assert.That(result).IsSameReferenceAs(expectedContext);
    }

    [Test]
    public async Task Resolve_MultipleResolvers_ReturnsFirstNonNull()
    {
        // Arrange
        var expectedContext = TestContext.Current!;
        TestContextResolverRegistry.Register(new NullResolver());
        TestContextResolverRegistry.Register(new FixedResolver(expectedContext));
        TestContextResolverRegistry.Register(new NullResolver()); // should not be reached

        // Act
        var result = TestContextResolverRegistry.Resolve();

        // Assert
        await Assert.That(result).IsSameReferenceAs(expectedContext);
    }

    [Test]
    public async Task Unregister_RemovesResolver()
    {
        // Arrange
        var expectedContext = TestContext.Current!;
        var resolver = new FixedResolver(expectedContext);
        TestContextResolverRegistry.Register(resolver);

        // Act
        var removed = TestContextResolverRegistry.Unregister(resolver);

        // Assert
        await Assert.That(removed).IsTrue();
        await Assert.That(TestContextResolverRegistry.Resolve()).IsNull();
    }

    [Test]
    public async Task Unregister_UnknownResolver_ReturnsFalse()
    {
        // Act
        var removed = TestContextResolverRegistry.Unregister(new NullResolver());

        // Assert
        await Assert.That(removed).IsFalse();
    }

    [Test]
    public async Task ContextCurrent_ConsultsResolversBeforeAsyncLocal()
    {
        // Arrange - register a resolver that returns the current test context
        // even when AsyncLocal is cleared
        var testContext = TestContext.Current!;
        TestContextResolverRegistry.Register(new FixedResolver(testContext));

        // Act - Context.Current should use our resolver
        var result = Context.Current;

        // Assert - should be our test context (from resolver), not some other context
        await Assert.That(result).IsSameReferenceAs(testContext);
    }

    [Test]
    public async Task ContextCurrent_CrossThread_ResolverProvidesContext()
    {
        // This test demonstrates the core use case: code running on a thread pool thread
        // (which does NOT inherit AsyncLocal) can still resolve the correct test context
        // via a custom resolver.

        var testContext = TestContext.Current!;

        // Store the test context ID in a thread-static or similar mechanism
        // that a resolver can read (simulating e.g. an MCP request context)
        var threadLocalResolver = new ThreadStaticResolver();
        TestContextResolverRegistry.Register(threadLocalResolver);

        Context? resolvedOnBackgroundThread = null;

        // Run on a fresh thread pool thread that does NOT inherit AsyncLocal
        var thread = new Thread(() =>
        {
            // Set the correlation data that the resolver will use
            ThreadStaticResolver.CurrentTestContext = testContext;

            // This would normally return GlobalContext since AsyncLocal is empty
            // but our resolver kicks in first
            resolvedOnBackgroundThread = Context.Current;

            ThreadStaticResolver.CurrentTestContext = null;
        });

        thread.Start();
        thread.Join();

        // Assert - the background thread resolved the correct context via our custom resolver
        await Assert.That(resolvedOnBackgroundThread).IsSameReferenceAs(testContext);
    }

    [Test]
    public async Task ContextCurrent_FallsBackToAsyncLocal_WhenResolverReturnsNull()
    {
        // Arrange - resolver returns null, so should fall back to AsyncLocal
        TestContextResolverRegistry.Register(new NullResolver());
        var asyncLocalContext = TestContext.Current;

        // Act
        var result = Context.Current;

        // Assert - should still resolve via AsyncLocal
        await Assert.That(result).IsSameReferenceAs(asyncLocalContext);
    }

    private class NullResolver : ITestContextResolver
    {
        public TestContext? ResolveCurrentTestContext() => null;
    }

    private class FixedResolver : ITestContextResolver
    {
        private readonly TestContext _context;

        public FixedResolver(TestContext context) => _context = context;

        public TestContext? ResolveCurrentTestContext() => _context;
    }

    /// <summary>
    /// A resolver that uses thread-static storage, simulating how a custom protocol
    /// (MCP, gRPC, etc.) might store the test context ID and resolve it.
    /// </summary>
    private class ThreadStaticResolver : ITestContextResolver
    {
        [ThreadStatic]
        public static TestContext? CurrentTestContext;

        public TestContext? ResolveCurrentTestContext() => CurrentTestContext;
    }
}
