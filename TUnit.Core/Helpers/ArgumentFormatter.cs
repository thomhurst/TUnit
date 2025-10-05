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
        var list = arguments as IList<object?> ?? arguments.ToList();
        if (list.Count == 0)
            return string.Empty;
            
        var formatted = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            formatted[i] = FormatDefault(list[i]);
        }
        return string.Join(", ", formatted);
    }

    private static string FormatDefault(object? o)
    {
        if (o is null)
        {
            return "null";
        }

        // Cache GetType() result to avoid repeated virtual method calls
        var type = o.GetType();

        // Handle tuples specially
        if (TupleHelper.IsTupleType(type))
        {
            return FormatTuple(o);
        }

        // Handle arrays and collections by showing their elements
        if (o is IEnumerable enumerable and not string)
        {
            return FormatEnumerable(enumerable);
        }

        var toString = o.ToString()!;

        if (o is Enum)
        {
            return toString;
        }

        if (type.IsPrimitive || o is string)
        {
            return toString;
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
        var elements = new List<string>(maxElements + 1);
        var count = 0;

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
