using System.Runtime.Versioning;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using LogLevel = TUnit.Core.Logging.LogLevel;

namespace TUnit.Engine.Logging;

[UnsupportedOSPlatform("browser")]
internal class TUnitFrameworkLogger(IExtension extension, IOutputDevice outputDevice, ILogger logger)
    : IOutputDeviceDataProducer, global::TUnit.Core.Logging.ILogger
{
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

        return Console.ForegroundColor;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logger.IsEnabled(MTPLoggerAdapter.Map(logLevel));
    }
}