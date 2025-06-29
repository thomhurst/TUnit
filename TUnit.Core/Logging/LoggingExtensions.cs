namespace TUnit.Core.Logging;

public static class LoggingExtensions
{
    internal static readonly Func<string, Exception?, string> Formatter =
        (state, exception) =>
            exception is not null
#pragma warning disable RS0030 // Do not use banned APIs
                ? $"{state}{Environment.NewLine}------Exception detail------{Environment.NewLine}{exception}"
#pragma warning restore RS0030 // Do not use banned APIs
                : state;

    public static ValueTask LogTraceAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Trace, message, null, Formatter);

    public static ValueTask LogDebugAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Debug, message, null, Formatter);

    public static ValueTask LogInformationAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Information, message, null, Formatter);

    public static ValueTask LogWarningAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Warning, message, null, Formatter);

    public static ValueTask LogErrorAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Error, message, null, Formatter);

    public static ValueTask LogErrorAsync(this ILogger logger, string message, Exception ex)
        => logger.LogAsync(LogLevel.Error, message, ex, Formatter);

    public static ValueTask LogErrorAsync(this ILogger logger, Exception ex)
    => logger.LogAsync(LogLevel.Error, ex.ToString(), null, Formatter);

    public static ValueTask LogCriticalAsync(this ILogger logger, string message)
        => logger.LogAsync(LogLevel.Critical, message, null, Formatter);

    public static void LogTrace(this ILogger logger, string message)
    => logger.Log(LogLevel.Trace, message, null, Formatter);

    public static void LogDebug(this ILogger logger, string message)
        => logger.Log(LogLevel.Debug, message, null, Formatter);

    public static void LogInformation(this ILogger logger, string message)
        => logger.Log(LogLevel.Information, message, null, Formatter);

    public static void LogWarning(this ILogger logger, string message)
        => logger.Log(LogLevel.Warning, message, null, Formatter);

    public static void LogError(this ILogger logger, string message)
        => logger.Log(LogLevel.Error, message, null, Formatter);

    public static void LogError(this ILogger logger, string message, Exception ex)
        => logger.Log(LogLevel.Error, message, ex, Formatter);

    public static void LogError(this ILogger logger, Exception ex)
        => logger.Log(LogLevel.Error, ex.ToString(), null, Formatter);

    public static void LogCritical(this ILogger logger, string message)
        => logger.Log(LogLevel.Critical, message, null, Formatter);
}
