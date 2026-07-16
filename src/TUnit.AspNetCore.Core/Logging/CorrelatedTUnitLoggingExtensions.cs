using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TUnit.Logging.Microsoft;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// Extension methods for adding correlated TUnit logging to a shared ASP.NET Core web application.
/// Registers both the <see cref="CorrelatedTUnitLoggerProvider"/> (from <c>TUnit.Logging.Microsoft</c>)
/// and the <see cref="TUnitTestContextMiddleware"/> for robust test context resolution via
/// both Activity baggage and the <c>X-TUnit-TestId</c> HTTP header.
/// </summary>
public static class CorrelatedTUnitLoggingExtensions
{
    /// <summary>
    /// Adds correlated TUnit logging to the service collection.
    /// This registers the <see cref="TUnitTestContextMiddleware"/> via an <see cref="IStartupFilter"/>
    /// and a <see cref="CorrelatedTUnitLoggerProvider"/> that writes <c>ILogger</c> output to
    /// <see cref="System.Console"/> on the calling thread so TUnit's console interceptor can route
    /// it to the correct test.
    /// Use with <see cref="TUnitTestIdHandler"/> on the client side to propagate the test context ID.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorrelatedTUnitLogging(
        this IServiceCollection services,
        LogLevel minLogLevel = LogLevel.Information)
    {
        services.AddSingleton<IStartupFilter>(new TUnitTestContextStartupFilter());
        services.AddSingleton<ILoggerProvider>(new CorrelatedTUnitLoggerProvider(minLogLevel));

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
