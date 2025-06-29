namespace TUnit.Core.Helpers;

internal static class ArgumentFormatter
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

    private static string FormatDefault(object? o)
    {
        if (o is null)
        {
            return "null";
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

        if (toString == o.GetType().FullName)
        {
            return o.GetType().Name;
        }

        return toString;
    }
}
