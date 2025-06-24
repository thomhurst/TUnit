namespace TUnit.Core.Helpers;

internal static class ArgumentFormatter
{
    public static string GetConstantValue(TestContext testContext, object? o)
    {
        foreach (var formatter in testContext.ArgumentDisplayFormatters)
        {
            var result = formatter(o);
            if (result != null)
            {
                return result;
            }
        }

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