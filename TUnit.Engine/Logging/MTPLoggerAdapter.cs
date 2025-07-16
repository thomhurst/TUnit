﻿using Microsoft.Testing.Platform.Logging;
using LogLevel = TUnit.Core.Logging.LogLevel;

namespace TUnit.Engine.Logging;

internal class MTPLoggerAdapter(ILogger logger) : Core.Logging.ILogger
{
    public async ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        await logger.LogAsync(Map(logLevel), state, exception, formatter);
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logger.Log(Map(logLevel), state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logger.IsEnabled(Map(logLevel));
    }

    public static LogLevel Map(Microsoft.Testing.Platform.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Microsoft.Testing.Platform.Logging.LogLevel.Trace => LogLevel.Trace,
            Microsoft.Testing.Platform.Logging.LogLevel.Debug => LogLevel.Debug,
            Microsoft.Testing.Platform.Logging.LogLevel.Information => LogLevel.Information,
            Microsoft.Testing.Platform.Logging.LogLevel.Warning => LogLevel.Warning,
            Microsoft.Testing.Platform.Logging.LogLevel.Error => LogLevel.Error,
            Microsoft.Testing.Platform.Logging.LogLevel.Critical => LogLevel.Critical,
            Microsoft.Testing.Platform.Logging.LogLevel.None => LogLevel.None,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }

    public static Microsoft.Testing.Platform.Logging.LogLevel Map(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Microsoft.Testing.Platform.Logging.LogLevel.Trace,
            LogLevel.Debug => Microsoft.Testing.Platform.Logging.LogLevel.Debug,
            LogLevel.Information => Microsoft.Testing.Platform.Logging.LogLevel.Information,
            LogLevel.Warning => Microsoft.Testing.Platform.Logging.LogLevel.Warning,
            LogLevel.Error => Microsoft.Testing.Platform.Logging.LogLevel.Error,
            LogLevel.Critical => Microsoft.Testing.Platform.Logging.LogLevel.Critical,
            LogLevel.None => Microsoft.Testing.Platform.Logging.LogLevel.None,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }
}
