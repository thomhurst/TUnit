namespace TUnit.Core;

/// <summary>
/// Advanced extension point for resolving which <see cref="TestContext"/> should receive console output
/// when the built-in <c>AsyncLocal</c> mechanism cannot determine the context.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Prefer <see cref="TestContext.MakeCurrent"/>.</strong> For most scenarios (gRPC handlers,
/// message queue consumers, MCP servers, etc.), call <see cref="TestContext.MakeCurrent"/> in your
/// handler after extracting the test ID. This sets the <c>AsyncLocal</c> directly and is simpler,
/// safer, and more efficient than implementing a resolver.
/// </para>
/// <para>
/// Implement this interface only when you need <em>automatic</em> protocol-level correlation
/// without requiring each handler to call <see cref="TestContext.MakeCurrent"/> explicitly
/// (for example, the built-in ASP.NET Core middleware).
/// </para>
/// <para>
/// <strong>Do not register a resolver per test.</strong> Resolvers must use ambient state
/// (e.g., <c>HttpContext.Items</c>, <c>Activity.Current</c>) to determine which test
/// is active. A resolver that always returns a fixed <see cref="TestContext"/> will produce
/// incorrect results when multiple tests run concurrently.
/// </para>
/// <para>
/// <strong>Performance and thread safety:</strong> This method is called on every
/// <c>Console.Write</c> / <c>Console.WriteLine</c> when no <c>AsyncLocal</c> context is available,
/// so implementations must be thread-safe and very cheap.
/// </para>
/// <para>
/// <strong>Ordering:</strong> In <see cref="Context.Current"/>, registered resolvers are consulted
/// only after all <c>AsyncLocal</c>-based contexts (test, build, class hook, assembly hook, session,
/// and discovery contexts) return <c>null</c>. Resolvers are a true last resort before
/// <see cref="GlobalContext"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Prefer MakeCurrent() for simple cases:
/// if (TestContext.GetById(testId) is { } ctx)
/// {
///     using (ctx.MakeCurrent())
///     {
///         await ProcessRequest();
///     }
/// }
///
/// // Use ITestContextResolver only for automatic protocol-level correlation:
/// public class McpTestContextResolver : ITestContextResolver
/// {
///     public TestContext? ResolveCurrentTestContext()
///     {
///         var testId = McpRequestContext.Current?.TestId;
///         return testId is not null ? TestContext.GetById(testId) : null;
///     }
/// }
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
