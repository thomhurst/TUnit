using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Core.Logging;

public class DefaultLogger(Context context) : TUnitLogger
{
    private readonly ConcurrentDictionary<string, List<string>> _values = new();

    /// <summary>
    /// Gets the context associated with this logger.
    /// </summary>
    protected Context Context => context;

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
        var list = _values.GetOrAdd(name, static _ =>
        [
        ]);
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
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = GenerateMessage(formatter(state, exception), exception, logLevel);

        await WriteToOutputAsync(message, logLevel >= LogLevel.Error);
    }

    public override void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = GenerateMessage(formatter(state, exception), exception, logLevel);

        WriteToOutput(message, logLevel >= LogLevel.Error);
    }

    /// <summary>
    /// Generates the formatted message to be logged.
    /// Override this method to customize the message format.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception associated with this log entry, if any.</param>
    /// <param name="logLevel">The log level.</param>
    /// <returns>The formatted message.</returns>
    protected virtual string GenerateMessage(string message, Exception? exception, LogLevel logLevel)
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

    /// <summary>
    /// Writes the message to the output.
    /// Override this method to customize how messages are written.
    /// </summary>
    /// <param name="message">The formatted message to write.</param>
    /// <param name="isError">True if this is an error-level message.</param>
    protected virtual void WriteToOutput(string message, bool isError)
    {
        if (isError)
        {
            context.ErrorOutputWriter.WriteLine(message);
            GlobalContext.Current.OriginalConsoleError.WriteLine(message);
        }
        else
        {
            context.OutputWriter.WriteLine(message);
            GlobalContext.Current.OriginalConsoleOut.WriteLine(message);
        }
    }

    /// <summary>
    /// Asynchronously writes the message to the output.
    /// Override this method to customize how messages are written.
    /// </summary>
    /// <param name="message">The formatted message to write.</param>
    /// <param name="isError">True if this is an error-level message.</param>
    /// <returns>A task representing the async operation.</returns>
    protected virtual async ValueTask WriteToOutputAsync(string message, bool isError)
    {
        if (isError)
        {
            await context.ErrorOutputWriter.WriteLineAsync(message);
            await GlobalContext.Current.OriginalConsoleError.WriteLineAsync(message);
        }
        else
        {
            await context.OutputWriter.WriteLineAsync(message);
            await GlobalContext.Current.OriginalConsoleOut.WriteLineAsync(message);
        }
    }
}
