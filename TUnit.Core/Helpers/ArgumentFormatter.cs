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

    public static string FormatArguments(IEnumerable<object?> arguments)
    {
        if (arguments is IList<object?> list)
        {
            if (list.Count == 0)
                return string.Empty;

            var formatted = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                formatted[i] = FormatDefault(list[i]);
            }
            return string.Join(", ", formatted);
        }

        var elements = new List<string>();
        foreach (var arg in arguments)
        {
            elements.Add(FormatDefault(arg));
        }

        return elements.Count == 0 ? string.Empty : string.Join(", ", elements);
    }

    private static string FormatDefault(object? o)
    {
        if (o is null)
        {
            return "null";
        }

        var type = o.GetType();

        if (TupleHelper.IsTupleType(type))
        {
            return FormatTuple(o);
        }

        if (o is IEnumerable enumerable and not string)
        {
            return FormatEnumerable(enumerable);
        }

        string toString;
        try
        {
            toString = o.ToString()!;
        }
        catch
        {
            // If ToString() throws, fall back to type name
            return type.Name;
        }

        if (o is Enum)
        {
            return toString;
        }

        if (type.IsPrimitive)
        {
            return toString;
        }

        if (o is string str)
        {
            // Replace dots with middle dot (·) to prevent VS Test Explorer from interpreting them as namespace separators
            // Only do this if the string contains dots, to avoid unnecessary allocations
            if (!str.Contains('.'))
            {
                return str;
            }

#if NET8_0_OR_GREATER
            // Use Span<char> for better performance - avoid string.Replace allocation
            Span<char> buffer = str.Length <= 256
                ? stackalloc char[str.Length]
                : new char[str.Length];

            for (int i = 0; i < str.Length; i++)
            {
                buffer[i] = str[i] == '.' ? '·' : str[i];
            }

            return new string(buffer);
#else
            return str.Replace(".", "·");
#endif
        }

        if (toString == type.FullName || toString == type.AssemblyQualifiedName)
        {
            return type.Name;
        }

        return toString;
    }

    private static string FormatTuple(object tuple)
    {
        var elements = TupleHelper.UnwrapTuple(tuple);
        var formatted = new string[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            formatted[i] = FormatDefault(elements[i]);
        }
        return $"({string.Join(", ", formatted)})";
    }

    private static string FormatEnumerable(IEnumerable enumerable)
    {
        const int maxElements = 10;
        Span<string?> elements = [null, null, null, null, null, null, null, null, null, null, null];

        var count = 0;
        try
        {
            foreach (var element in enumerable)
            {
                if (count >= maxElements)
                {
                    elements[count] ="...";
                    break;
                }
                elements[count] = FormatDefault(element);
                count++;
            }
        }
        catch
        {
            // If GetEnumerator() or MoveNext() throws, fall back to type name
            return enumerable.GetType().Name;
        }

        return string.Join(", ", elements[..count]);
    }
}
