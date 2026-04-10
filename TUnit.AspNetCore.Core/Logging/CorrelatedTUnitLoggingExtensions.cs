using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// Extension methods for adding TUnit test context middleware to a shared web application.
/// </summary>
public static class CorrelatedTUnitLoggingExtensions
{
    /// <summary>
    /// Adds the <see cref="TUnitTestContextMiddleware"/> to the pipeline via an <see cref="IStartupFilter"/>
    /// and a synchronous logger provider that writes <c>ILogger</c> output to <see cref="System.Console"/>
    /// on the calling thread so TUnit's console interceptor can route it to the correct test.
    /// Use with <see cref="TUnitTestIdHandler"/> on the client side to propagate the test context ID.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorrelatedTUnitLogging(
        this IServiceCollection services)
    {
        services.AddSingleton<IStartupFilter>(new TUnitTestContextStartupFilter());
        services.AddSingleton<ILoggerProvider>(new SynchronousTUnitLoggerProvider());

        return services;
    }
}

/// <summary>
/// Startup filter that adds <see cref="TUnitTestContextMiddleware"/> early in the pipeline.
/// </summary>
internal sealed class TUnitTestContextStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<TUnitTestContextMiddleware>();
            next(app);
        };
    }
}
