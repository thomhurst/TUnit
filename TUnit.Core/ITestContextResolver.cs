namespace TUnit.Core;

/// <summary>
/// Allows custom logic for resolving which <see cref="TestContext"/> should receive console output
/// when the built-in <see cref="AsyncLocal{T}"/>-based mechanism cannot determine the context.
/// </summary>
/// <remarks>
/// <para>
/// The built-in context resolution uses <c>AsyncLocal&lt;T&gt;</c> which works when code runs on the same
/// async execution flow as the test. However, it returns <c>null</c> when shared services (e.g., hosted services,
/// gRPC handlers, message queue consumers) process work on their own thread pool threads.
/// Registered resolvers act as a fallback, consulted only when the <c>AsyncLocal</c> chain yields no result.
/// </para>
/// <para>
/// Implement this interface and register it via <see cref="TestContextResolverRegistry.Register"/>
/// to provide custom resolution logic for these scenarios.
/// </para>
/// <para>
/// <strong>Performance and thread safety:</strong> This method is called on every
/// <c>Console.Write</c> / <c>Console.WriteLine</c> from arbitrary threads,
/// so implementations must be thread-safe and very cheap. Avoid allocations, locks, and I/O in the hot path.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class McpTestContextResolver : ITestContextResolver
/// {
///     public TestContext? ResolveCurrentTestContext()
///     {
///         var testId = McpRequestContext.Current?.TestId;
///         return testId is not null ? TestContext.GetById(testId) : null;
///     }
/// }
///
/// // Register in a [Before(Assembly)] hook:
/// TestContextResolverRegistry.Register(new McpTestContextResolver());
/// </code>
/// </example>
public interface ITestContextResolver
{
    /// <summary>
    /// Attempts to resolve the current test context.
    /// Return <c>null</c> to fall through to the next resolver or the built-in <c>AsyncLocal</c> chain.
    /// </summary>
    /// <returns>The resolved <see cref="TestContext"/>, or <c>null</c> if this resolver cannot determine the context.</returns>
    TestContext? ResolveCurrentTestContext();
}
