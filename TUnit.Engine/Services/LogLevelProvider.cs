using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine.Services;

public class LogLevelProvider(ICommandLineOptions commandLineOptions)
{
    internal static LogLevel? _logLevel;
    public LogLevel LogLevel => _logLevel ??= GetLogLevel();

    private LogLevel GetLogLevel()
    {
        if (commandLineOptions.TryGetOptionArgumentList(LogLevelCommandProvider.LogLevelOption, out var values))
        {
            return LogLevelCommandProvider.ParseLogLevel(values);
        }

        return LogLevel.Information;
    }
}
