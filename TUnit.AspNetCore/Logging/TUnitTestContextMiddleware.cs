using Microsoft.AspNetCore.Http;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// Middleware that extracts the TUnit test context ID from incoming HTTP request headers
/// and stores the associated <see cref="TestContext"/> in <see cref="HttpContext.Items"/>
/// for correlated logging.
/// </summary>
public sealed class TUnitTestContextMiddleware
{
    /// <summary>
    /// The key used to store the <see cref="TestContext"/> in <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string HttpContextKey = "TUnit.TestContext";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates a new <see cref="TUnitTestContextMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public TUnitTestContextMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// Invokes the middleware, extracting the test context from the request header if present.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(TUnitTestIdHandler.HeaderName, out var values)
            && values.FirstOrDefault() is { } testId
            && TestContext.GetById(testId) is { } testContext)
        {
            httpContext.Items[HttpContextKey] = testContext;
        }

        await _next(httpContext);
    }
}
