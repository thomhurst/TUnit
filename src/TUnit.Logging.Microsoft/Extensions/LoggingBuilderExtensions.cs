using global::Microsoft.Extensions.Logging;
using TUnit.Core;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// Extension methods for <see cref="ILoggingBuilder"/> to add TUnit logging.
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds the TUnit logger provider to the logging builder.
    /// Logs will be written to the specified test context's output.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="context">The test context to write logs to.</param>
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
