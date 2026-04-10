using Microsoft.AspNetCore.Http;
using TUnit.Core;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// Middleware that extracts the TUnit test context ID from incoming HTTP request headers
/// and calls <see cref="TestContext.MakeCurrent"/> so that console output and log routing
/// within the request are attributed to the correct test.
/// </summary>
public sealed class TUnitTestContextMiddleware
{
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
            using (testContext.MakeCurrent())
            {
                await _next(httpContext);
            }

            return;
        }

        await _next(httpContext);
    }
}
