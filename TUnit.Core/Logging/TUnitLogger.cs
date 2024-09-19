using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Core.Logging;

public abstract class TUnitLogger
{
    private readonly ConcurrentDictionary<string, List<string>> _values = new();
    internal LogLevel RequestedLogLevel { get; set; }

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

        if (value.GetType().IsPrimitive)
        {
            return value.ToString() ?? "null";
        }

#if NET8_0_OR_GREATER
        return System.Text.Json.JsonSerializer.Serialize(value) ?? "null";
#endif

        throw new NotImplementedException();
    }
    
    private bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return false;
        }
        
        return logLevel >= RequestedLogLevel;
    }
    
    protected abstract void Log(string message);

    public void LogTrace(string message)
    {
        if (IsEnabled(LogLevel.Trace))
        {
            FormatAndLog(message, null);
        }
    }
    public void LogTrace(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Trace))
        {
            FormatAndLog(message, exception);
        }
    }
    
    public void LogDebug(string message)
    {
        if (IsEnabled(LogLevel.Debug))
        {
            FormatAndLog(message, null);
        }
    }
    
    public void LogDebug(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Debug))
        {
            FormatAndLog(message, exception);
        }
    }
    
    public void LogInformation(string message)
    {
        if (IsEnabled(LogLevel.Information))
        {
            FormatAndLog(message, null);
        }
    }
    
    public void LogInformation(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Information))
        {
            FormatAndLog(message, exception);
        }
    }
    
    public void LogWarning(string message)
    {
        if (IsEnabled(LogLevel.Warning))
        {
            FormatAndLog(message, null);
        }
    }
    
    public void LogWarning(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Warning))
        {
            FormatAndLog(message, exception);
        }
    }
    
    public void LogError(string message)
    {
        if (IsEnabled(LogLevel.Error))
        {
            FormatAndLog(message, null);
        }
    }
    
    public void LogError(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Error))
        {
            FormatAndLog(message, exception);
        }
    }
    
    public void LogCritical(string message)
    {
        if (IsEnabled(LogLevel.Critical))
        {
            FormatAndLog(message, null);
        }
    }
    
    public void LogCritical(string message, Exception exception)
    {
        if (IsEnabled(LogLevel.Critical))
        {
            FormatAndLog(message, exception);
        }
    }

    private void FormatAndLog(string message)
    {
        FormatAndLog(message, null);
    }
    
    internal void FormatAndLog(string message, Exception? exception)
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
            stringBuilder.AppendLine("--- Context ---");
            
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

        Log(stringBuilder.ToString());
    }
}