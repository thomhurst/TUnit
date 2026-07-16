using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine.Services;

public class LogLevelProvider
{
    private readonly LogLevel _logLevel;

    public LogLevelProvider(ICommandLineOptions commandLineOptions)
    {
        _logLevel = GetLogLevel(commandLineOptions);
    }

    public LogLevel LogLevel => _logLevel;

    private static LogLevel GetLogLevel(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList(LogLevelCommandProvider.LogLevelOption, out var values))
        {
            return LogLevelCommandProvider.ParseLogLevel(values);
        }

        return LogLevel.Information;
    }
}
