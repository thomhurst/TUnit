using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core.Enums;

namespace TUnit.Engine.Services;

public class LogLevelProvider(ICommandLineOptions commandLineOptions)
{
    internal static LogLevel? _logLevel;
    public LogLevel LogLevel => _logLevel ??= GetLogLevel();

    private LogLevel GetLogLevel()
    {
        if (commandLineOptions.TryGetOptionArgumentList("log-level", out var values)
            && Enum.TryParse<LogLevel>(values.FirstOrDefault(), out var parsedResult))
        {
            return parsedResult;
        }

        return LogLevel.Information;
    }
}