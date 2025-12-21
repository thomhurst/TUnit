using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TUnit.AspNetCore.Logging;
using TUnit.Core;

namespace TUnit.AspNetCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to simplify service replacement in tests.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds the TUnit logger provider to the logging builder with a specific context provider.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="context">The test context.</param>
    /// <param name="minLogLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddTUnit(
        this ILoggingBuilder builder,
        TestContext context,
        LogLevel minLogLevel = LogLevel.Information)
    {
        builder.AddProvider(new TUnitLoggerProvider(context, minLogLevel));
        return builder;
    }
}
