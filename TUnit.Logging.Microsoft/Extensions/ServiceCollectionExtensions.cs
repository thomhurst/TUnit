using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add TUnit logging.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TUnit logging to the service collection.
    /// Logs will be written to the specified test context's output.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="context">The test context to write logs to.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTUnitLogging(
        this IServiceCollection services,
        TestContext context,
        LogLevel minLogLevel = LogLevel.Information)
    {
        services.AddLogging(builder => builder.AddTUnit(context, minLogLevel));
        return services;
    }
}
