using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace TUnit.AspNetCore.Interception;

/// <summary>
/// Extension methods for adding HTTP exchange capture to tests.
/// </summary>
public static class HttpExchangeCaptureExtensions
{
    /// <summary>
    /// Adds HTTP exchange capture to the service collection.
    /// This registers both the capture store and a startup filter that adds the middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for capture settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void ConfigureTestServices(IServiceCollection services)
    /// {
    ///     services.AddHttpExchangeCapture();
    /// }
    ///
    /// [Test]
    /// public async Task Test()
    /// {
    ///     var client = Factory.CreateClient();
    ///     await client.GetAsync("/api/todos");
    ///
    ///     var capture = Services.GetRequiredService&lt;HttpExchangeCapture&gt;();
    ///     await Assert.That(capture.Last!.Response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddHttpExchangeCapture(
        this IServiceCollection services,
        Action<HttpExchangeCapture>? configure = null)
    {
        var capture = new HttpExchangeCapture();
        configure?.Invoke(capture);

        services.AddSingleton(capture);
        services.AddSingleton<IStartupFilter>(new HttpExchangeCaptureStartupFilter());

        return services;
    }

    /// <summary>
    /// Adds the HTTP exchange capture middleware to the pipeline.
    /// Prefer using <see cref="AddHttpExchangeCapture"/> which handles this automatically.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseHttpExchangeCapture(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpExchangeCaptureMiddleware>();
    }
}

/// <summary>
/// Startup filter that adds the HTTP exchange capture middleware early in the pipeline.
/// </summary>
internal sealed class HttpExchangeCaptureStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Add capture middleware first so it captures everything
            app.UseMiddleware<HttpExchangeCaptureMiddleware>();
            next(app);
        };
    }
}
