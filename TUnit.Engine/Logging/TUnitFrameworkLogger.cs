using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

internal class TUnitFrameworkLogger : IOutputDeviceDataProducer, ITUnitFrameworkLogger
{
    private readonly IExtension _extension;
    private readonly IOutputDevice _outputDevice;
    private readonly ILogger<TUnitFrameworkLogger> _logger;

    public static TUnitFrameworkLogger? Instance { get; private set; }

    public TUnitFrameworkLogger(IExtension extension, IOutputDevice outputDevice, ILoggerFactory loggerFactory)
    {
        _extension = extension;
        _outputDevice = outputDevice;
        _logger = loggerFactory.CreateLogger<TUnitFrameworkLogger>();

        Instance = this;
    }

    public async Task LogInformationAsync(string text)
    {
        await Task.WhenAll(
        [
            _outputDevice.DisplayAsync(this, new TextOutputDeviceData(text)),

            _logger.LogErrorAsync(text),
        ]);
    }
    
    public async Task LogWarningAsync(string text)
    {
        await Task.WhenAll(
        [
            _outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
            {
                ForegroundColor = new SystemConsoleColor
                {
                    ConsoleColor = ConsoleColor.DarkYellow
                }
            }),

            _logger.LogWarningAsync(text),
        ]);
    }

    public async Task LogErrorAsync(string text)
    {
        await Task.WhenAll(
        [
            _outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(text)
            {
                ForegroundColor = new SystemConsoleColor
                {
                    ConsoleColor = ConsoleColor.DarkRed
                }
            }),

            _logger.LogErrorAsync(text),
        ]);
    }
    
    public async Task LogErrorAsync(Exception exception)
    {
        await Task.WhenAll(
        [
            _outputDevice.DisplayAsync(this, new FormattedTextOutputDeviceData(exception.ToString())
            {
                ForegroundColor = new SystemConsoleColor
                {
                    ConsoleColor = ConsoleColor.DarkRed
                }
            }),

            _logger.LogErrorAsync(exception),
        ]);
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(true);
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
}