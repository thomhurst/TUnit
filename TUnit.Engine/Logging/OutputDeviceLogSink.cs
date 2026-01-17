using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// A built-in log sink that streams log messages to IDEs (Rider, VS, etc.)
/// via Microsoft Testing Platform's IOutputDevice.
/// </summary>
internal class OutputDeviceLogSink : ILogSink, IOutputDeviceDataProducer
{
    private readonly IOutputDevice _outputDevice;
    private readonly LogLevel _minLevel;
    private readonly IExtension _extension;

    public OutputDeviceLogSink(IOutputDevice outputDevice, IExtension extension, LogLevel minLevel = LogLevel.Information)
    {
        _outputDevice = outputDevice;
        _extension = extension;
        _minLevel = minLevel;
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        // Fire and forget - IOutputDevice is async-only
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        var formattedMessage = FormatMessage(message, exception);
        var color = GetConsoleColor(level);

        await _outputDevice.DisplayAsync(
            this,
            new FormattedTextOutputDeviceData(formattedMessage)
            {
                ForegroundColor = new SystemConsoleColor { ConsoleColor = color }
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    private static string FormatMessage(string message, Exception? exception)
    {
        if (exception is null)
        {
            return message;
        }

        return $"{message}{Environment.NewLine}{exception}";
    }

    private static ConsoleColor GetConsoleColor(LogLevel level) => level switch
    {
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Debug => ConsoleColor.Gray,
        _ => ConsoleColor.White
    };
}
