using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Core.Logging;

public class DefaultLogger : TUnitLogger
{
    private readonly ConcurrentDictionary<string, List<string>> _values = new();

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


    public override async ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = GenerateMessage(formatter(state, exception), exception, logLevel);
        
        if (logLevel >= LogLevel.Error)
        {
            await Console.Error.WriteLineAsync(message);
        }
        else
        {
            await Console.Out.WriteLineAsync(message);
        }
    }

    public override void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = GenerateMessage(formatter(state, exception), exception, logLevel);
        
        if (logLevel >= LogLevel.Error)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    private string GenerateMessage(string message, Exception? exception, LogLevel logLevel)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"{logLevel.ToString()}: ");
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

        return builtString;
    }
}