using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Engine.Services;
using LogLevel = TUnit.Core.Logging.LogLevel;

namespace TUnit.Engine.Logging;

public class TUnitFrameworkLogger(IExtension extension, IOutputDevice outputDevice, ILogger logger, VerbosityService verbosityService)
    : IOutputDeviceDataProducer, Core.Logging.ILogger
{
    private readonly bool _hideTestOutput = verbosityService.HideTestOutput;

    private readonly MTPLoggerAdapter _adapter = new(logger);

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(true);
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;

    public async ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var text = formatter(state, exception);

        await outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
        {
            ForegroundColor = new SystemConsoleColor
            {
                ConsoleColor = GetConsoleColor(logLevel)
            }
        });

        await _adapter.LogAsync(logLevel, state, exception, formatter);
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var text = formatter(state, exception);

        outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
        {
            ForegroundColor = new SystemConsoleColor
            {
                ConsoleColor = GetConsoleColor(logLevel)
            }
        });

        _adapter.Log(logLevel, state, exception, formatter);
    }

    private static ConsoleColor GetConsoleColor(LogLevel logLevel)
    {
        if (logLevel == LogLevel.Warning)
        {
            return ConsoleColor.DarkYellow;
        }

        if (logLevel >= LogLevel.Error)
        {
            return ConsoleColor.DarkRed;
        }

        // Console.ForegroundColor is not supported on browser platforms
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsBrowser())
        {
            return Console.ForegroundColor;
        }
        return ConsoleColor.Gray; // Default color for browser platforms
#else
        return Console.ForegroundColor;
#endif
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return !_hideTestOutput && logger.IsEnabled(MTPLoggerAdapter.Map(logLevel));
    }

    public async Task LogErrorAsync(string message)
    {
        await LogAsync(LogLevel.Error, message, null, (s, _) => s);
    }

    public async Task LogErrorAsync(Exception exception)
    {
        await LogAsync(LogLevel.Error, exception.Message, exception, (s, e) => e?.ToString() ?? s);
    }
}
