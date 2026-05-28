using System.Collections;
using System.Text;

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

    public static string Format(object? o, Type? parameterType, List<Func<object?, string?>> formatters)
    {
        foreach (var formatter in formatters)
        {
            var result = formatter(o);
            if (result != null)
            {
                return result;
            }
        }

        return FormatDefault(o, parameterType);
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

            if (list.Count == 1)
                return FormatDefault(list[0]);

            var builder = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(FormatDefault(list[i]));
            }
            return builder.ToString();
        }

        // Non-IList fallback (rare: callers normally pass object?[]). Build directly into a
        // StringBuilder rather than allocating an intermediate List<string> + string.Join.
        var fallbackBuilder = new StringBuilder();
        foreach (var arg in arguments)
        {
            if (fallbackBuilder.Length > 0)
            {
                fallbackBuilder.Append(", ");
            }
            fallbackBuilder.Append(FormatDefault(arg));
        }
        return fallbackBuilder.ToString();
    }

    private static string FormatDefault(object? o)
    {
        return FormatDefault(o, parameterType: null);
    }

    private static string FormatDefault(object? o, Type? parameterType)
    {
        if (o is null)
        {
            return "null";
        }

        var type = o.GetType();

        // If the value is a numeric type but the parameter type is an enum,
        // convert to the enum for display purposes (e.g., MatrixDataSource
        // stores enum values as their underlying numeric type)
        var resolvedParameterType = parameterType != null ? Nullable.GetUnderlyingType(parameterType) ?? parameterType : null;
        if (resolvedParameterType is { IsEnum: true } && !type.IsEnum)
        {
            try
            {
                var enumValue = Enum.ToObject(resolvedParameterType, o);
                return enumValue.ToString() ?? type.Name;
            }
            catch (ArgumentException)
            {
                // Value cannot be converted to the enum type - fall through to default formatting
            }
        }

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
            return str.Replace(".", "·");
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

        if (elements.Length == 0)
            return "()";

        if (elements.Length == 1)
            return $"({FormatDefault(elements[0])})";

        var builder = new StringBuilder("(");
        for (int i = 0; i < elements.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append(FormatDefault(elements[i]));
        }
        builder.Append(')');
        return builder.ToString();
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
