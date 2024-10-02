using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using LogLevel = TUnit.Core.Logging.LogLevel;

namespace TUnit.Engine.Logging;

internal class TUnitFrameworkLogger : IOutputDeviceDataProducer, global::TUnit.Core.Logging.ILogger
{
    private readonly IExtension _extension;
    private readonly IOutputDevice _outputDevice;
    private readonly ILogger _logger;
    
    public TUnitFrameworkLogger(IExtension extension, IOutputDevice outputDevice, ILogger logger)
    {
        _extension = extension;
        _outputDevice = outputDevice;
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(true);
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    
    public async Task LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var text = formatter(state, exception);

        await _outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
        {
            ForegroundColor = new SystemConsoleColor
            {
                ConsoleColor = GetConsoleColor(logLevel)
            }
        });

        if (exception is not null)
        {
            await _logger.LogErrorAsync(text, exception);
        }
        else
        {
            await _logger.LogErrorAsync(text);
        }
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var text = formatter(state, exception);

        _outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
        {
            ForegroundColor = new SystemConsoleColor
            {
                ConsoleColor = GetConsoleColor(logLevel)
            }
        });

        if (exception is not null)
        {
            _logger.LogError(text, exception);
        }
        else
        {
            _logger.LogError(text);
        }
    }

    private static ConsoleColor GetConsoleColor(LogLevel logLevel)
    {
        if (logLevel == LogLevel.Warning)
        {
            return ConsoleColor.DarkYellow;
        }
        
        if(logLevel >= LogLevel.Error)
        {
            return ConsoleColor.DarkRed;
        }

        return Console.ForegroundColor;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(MTPLoggerAdapter.Map(logLevel));
    }
}