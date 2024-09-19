using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;

namespace TUnit.Engine.Services;

public class LogLevelProvider
{
    private readonly ICommandLineOptions _commandLineOptions;

    internal static LogLevel? _logLevel;
    public LogLevel LogLevel => _logLevel ??= GetLogLevel();

    private LogLevel GetLogLevel()
    {
        if (_commandLineOptions.TryGetOptionArgumentList("log-level", out var values)
            && Enum.TryParse<LogLevel>(values.FirstOrDefault(), out var parsedResult))
        {
            return parsedResult;
        }

        return LogLevel.Information;
    }

    public LogLevelProvider(ICommandLineOptions commandLineOptions)
    {
        _commandLineOptions = commandLineOptions;
    }
}