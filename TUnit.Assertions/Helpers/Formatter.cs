using System.Collections;
using System.Globalization;

namespace TUnit.Assertions.Helpers;

internal abstract class Formatter
{
    private class SimpleFormatter<T>(Func<T, string> formatValue) : Formatter
    {
        public override bool CanHandle(object? value)
        {
            return value is T;
        }

        public override string FormatValue(object? value)
        {
            return formatValue.Invoke((T)value!);
        }
    }

    public abstract bool CanHandle(object? value);
    public abstract string FormatValue(object? value);

    private static readonly List<Formatter> Formatters = [
        new SimpleFormatter<bool>(value => value ? "true" : "false"),
        new SimpleFormatter<string>(value => $"\"{value}\""),
        new SimpleFormatter<char>(value => $"'{value}'"),
        new SimpleFormatter<DateTime>(value => $"<{value:O}>"),
        new SimpleFormatter<DateTimeOffset>(value => value.ToString("<yyyy-MM-dd HH:mm:ss.fff tt>", CultureInfo.InvariantCulture)),
        new SimpleFormatter<DateOnly>(value => value.ToString("<yyyy-MM-dd>", CultureInfo.InvariantCulture)),
        new SimpleFormatter<TimeOnly>(value => value.ToString("<HH:mm:ss.fff>", CultureInfo.InvariantCulture)),
        new SimpleFormatter<IEnumerable>(value => $"[{string.Join(", ", value.Cast<object>().Select(Format))}]")
    ];

    public static string Format(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        var formatter = Formatters.FirstOrDefault(f => f.CanHandle(value));
        if (formatter != null)
        {
            return formatter.FormatValue(value);
        }

        return value.ToString() ?? "null";
    }
}
