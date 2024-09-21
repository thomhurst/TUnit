using System.Collections.Concurrent;
using System.Text;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Logging;

public abstract class TUnitLogger
{
    private readonly ConcurrentDictionary<string, List<string>> _values = new();

    protected abstract bool WriteToContext { get; }

    public void PushProperties(IDictionary<string, List<object>> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
            foreach (var value in keyValuePair.Value)
            {
                PushProperty(keyValuePair.Key, value);
            }
        }
    }
    
    public void PushProperty(string name, object? value)
    {
        var list = _values.GetOrAdd(name, _ => new());
        var formattedValue = FormatValue(value);
        list.Add(formattedValue);
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string strValue)
        {
            return strValue;
        }
        
        return value.ToString() ?? "null";
    }
    
    private bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return false;
        }
        
        return logLevel >= GlobalContext.LogLevel;
    }
    
    protected abstract void Log(IContext? currentContext, string message);

    public void LogTrace(string message)
    {
        if (IsEnabled(LogLevel.Trace))
        {
            FormatAndLog(message, null, LogLevel.Trace);
        }
    }
    public void LogTrace(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Trace))
        {
            FormatAndLog(message, exception, LogLevel.Trace);
        }
    }
    
    public void LogDebug(string message)
    {
        if (IsEnabled(LogLevel.Debug))
        {
            FormatAndLog(message, null, LogLevel.Debug);
        }
    }
    
    public void LogDebug(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Debug))
        {
            FormatAndLog(message, exception, LogLevel.Debug);
        }
    }
    
    public void LogInformation(string message)
    {
        if (IsEnabled(LogLevel.Information))
        {
            FormatAndLog(message, null, LogLevel.Information);
        }
    }
    
    public void LogInformation(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Information))
        {
            FormatAndLog(message, exception, LogLevel.Information);
        }
    }
    
    public void LogWarning(string message)
    {
        if (IsEnabled(LogLevel.Warning))
        {
            FormatAndLog(message, null, LogLevel.Warning);
        }
    }
    
    public void LogWarning(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Warning))
        {
            FormatAndLog(message, exception, LogLevel.Warning);
        }
    }
    
    public void LogError(string message)
    {
        if (IsEnabled(LogLevel.Error))
        {
            FormatAndLog(message, null, LogLevel.Error);
        }
    }
    
    public void LogError(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Error))
        {
            FormatAndLog(message, exception, LogLevel.Error);
        }
    }
    
    public void LogCritical(string message)
    {
        if (IsEnabled(LogLevel.Critical))
        {
            FormatAndLog(message, null, LogLevel.Critical);
        }
    }
    
    public void LogCritical(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Critical))
        {
            FormatAndLog(message, exception, LogLevel.Critical);
        }
    }
    
    internal void FormatAndLog(string message, Exception? exception, LogLevel logLevel)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(message);
        stringBuilder.AppendLine();
        
        if (exception is not null)
        {
            stringBuilder.AppendLine(exception.ToString());
            stringBuilder.AppendLine();
        }

        if (_values.Any())
        {
            stringBuilder.AppendLine("--- Properties ---");
            
            foreach (var keyValuePair in _values)
            {
                stringBuilder.AppendLine($"{keyValuePair.Key}:");

                foreach (var value in keyValuePair.Value)
                {
                    stringBuilder.AppendLine($"\t{value}");
                }

                stringBuilder.AppendLine();
            }
        }

        var builtString = stringBuilder.ToString();
        
        var currentContext = Context.Current;

#if NET8_0_OR_GREATER
        if (WriteToContext)
        {
            var writer = logLevel >= LogLevel.Error
                ? currentContext?.ErrorOutputWriter : currentContext?.OutputWriter;
            
            writer?.WriteLine(builtString);
        }
#endif
        
        Log(currentContext, builtString);
    }
}