using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TUnit.AspNetCore.Logging;

/// <summary>
/// Extension methods for adding correlated TUnit logging to a shared web application.
/// </summary>
public static class CorrelatedTUnitLoggingExtensions
{
    /// <summary>
    /// Adds correlated TUnit logging to the service collection.
    /// This registers the <see cref="TUnitTestContextMiddleware"/> via an <see cref="IStartupFilter"/>
    /// and a <see cref="CorrelatedTUnitLoggerProvider"/> that resolves the test context per log call.
    /// Use with <see cref="TUnitTestIdHandler"/> on the client side to propagate test context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorrelatedTUnitLogging(
        this IServiceCollection services,
        LogLevel minLogLevel = LogLevel.Information)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IStartupFilter>(new TUnitTestContextStartupFilter());
        services.AddSingleton<ILoggerProvider>(sp =>
            new CorrelatedTUnitLoggerProvider(
                sp.GetRequiredService<IHttpContextAccessor>(),
                minLogLevel));

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
