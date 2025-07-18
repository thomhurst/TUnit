using System.Collections;

namespace TUnit.Core.Helpers;

public static class ArgumentFormatter
{
    public static string Format(object? o, List<Func<object?, string?>> formatters)
    {
        foreach (var formatter in formatters)
        {
            var result = formatter(o);
            if (result != null)
            {
                return result;
            }
        }

        return FormatDefault(o);
    }

    public static string GetConstantValue(TestContext testContext, object? o)
    {
        return Format(o, testContext.ArgumentDisplayFormatters);
    }

    /// <summary>
    /// Formats a collection of arguments into a display string.
    /// </summary>
    public static string FormatArguments(IEnumerable<object?> arguments)
    {
        return string.Join(", ", arguments.Select(arg => FormatDefault(arg)));
    }

    private static string FormatDefault(object? o)
    {
        if (o is null)
        {
            return "null";
        }

        // Handle arrays and collections by showing their elements
        if (o is IEnumerable enumerable && o is not string)
        {
            return FormatEnumerable(enumerable);
        }

        var toString = o.ToString()!;

        if (o is Enum)
        {
            return toString;
        }

        if (o.GetType().IsPrimitive || o is string)
        {
            return toString;
        }

        if (toString == o.GetType().FullName || toString == o.GetType().AssemblyQualifiedName)
        {
            return o.GetType().Name;
        }

        return toString;
    }

    /// <summary>
    /// Formats an enumerable collection for display.
    /// </summary>
    private static string FormatEnumerable(IEnumerable enumerable)
    {
        var elements = new List<string>();
        var count = 0;
        const int maxElements = 10; // Limit to prevent huge displays

        foreach (var element in enumerable)
        {
            if (count >= maxElements)
            {
                elements.Add("...");
                break;
            }
            elements.Add(FormatDefault(element));
            count++;
        }

        return string.Join(", ", elements);
    }
}
