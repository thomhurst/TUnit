using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// Extension methods for adding correlated TUnit logging.
/// The correlated logger dynamically resolves the current test context per log call
/// via <see cref="TUnit.Core.TestContext.Current"/> (AsyncLocal or Activity baggage fallback),
/// enabling shared service scenarios where a single host serves multiple tests.
/// </summary>
public static class CorrelatedLoggingExtensions
{
    /// <summary>
    /// Adds a <see cref="CorrelatedTUnitLoggerProvider"/> that dynamically resolves the current
    /// test context for each log call. Use this for shared service hosts (ASP.NET Core, gRPC, etc.)
    /// where a single host serves multiple tests running in parallel.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorrelatedTUnitLogging(
        this IServiceCollection services,
        LogLevel minLogLevel = LogLevel.Information)
    {
        services.AddSingleton<ILoggerProvider>(new CorrelatedTUnitLoggerProvider(minLogLevel));
        return services;
    }

    /// <summary>
    /// Adds a <see cref="CorrelatedTUnitLoggerProvider"/> to the logging builder.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddCorrelatedTUnit(
        this ILoggingBuilder builder,
        LogLevel minLogLevel = LogLevel.Information)
    {
        builder.AddProvider(new CorrelatedTUnitLoggerProvider(minLogLevel));
        return builder;
    }
}
