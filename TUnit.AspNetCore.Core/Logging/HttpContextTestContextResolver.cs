using Microsoft.AspNetCore.Http;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// An <see cref="ITestContextResolver"/> that resolves the current test context from
/// <see cref="HttpContext.Items"/>, where it was stored by <see cref="TUnitTestContextMiddleware"/>.
/// </summary>
/// <remarks>
/// <para>
/// This resolver is automatically registered when <see cref="CorrelatedTUnitLoggingExtensions.AddCorrelatedTUnitLogging"/>
/// is called. It enables <see cref="Context.Current"/> (and therefore the console interceptor)
/// to route output to the correct test even on ASP.NET Core request-processing threads
/// that don't inherit the test's <c>AsyncLocal</c> context.
/// </para>
/// <para>
/// Resolution cost: one <see cref="IHttpContextAccessor.HttpContext"/> property access
/// plus one dictionary lookup. Both are very cheap.
/// </para>
/// </remarks>
public sealed class HttpContextTestContextResolver : ITestContextResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates a new <see cref="HttpContextTestContextResolver"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HttpContextTestContextResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public TestContext? ResolveCurrentTestContext()
    {
        if (_httpContextAccessor.HttpContext?.Items is { } items
            && items.TryGetValue(TUnitTestContextMiddleware.HttpContextKey, out var value)
            && value is TestContext testContext)
        {
            return testContext;
        }

        return null;
    }
}
