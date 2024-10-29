namespace TUnit.Core.Helpers;

internal static class ArgumentFormatter
{
    public static string GetConstantValue(TestContext testContext, object? o)
    {
        if (testContext.ArgumentDisplayFormatters.FirstOrDefault(x => x.CanHandle(o)) is { } validFormatter)
        {
            return validFormatter.FormatValue(o);
        }

        if (o is null)
        {
            return "null";
        }

        if (o is Enum @enum)
        {
            return @enum.ToString();
        }
        
        if (o.GetType().IsPrimitive || o is string)
        {
            return o.ToString()!;
        }

        return o.GetType().Name;
    }
}