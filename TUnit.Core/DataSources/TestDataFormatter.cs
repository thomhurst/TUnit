using TUnit.Core.Helpers;

namespace TUnit.Core.DataSources;

/// <summary>
/// AOT-compatible formatter for test data that works with metadata
/// </summary>
public static class TestDataFormatter
{
    /// <summary>
    /// Formats test arguments for display using the test context's formatters
    /// </summary>
    public static string FormatArguments(TestContext context)
    {
        var arguments = context.TestDetails.TestMethodArguments;
        return FormatArguments(arguments, context.ArgumentDisplayFormatters);
    }

    /// <summary>
    /// Formats an array of arguments using the provided formatters
    /// </summary>
    public static string FormatArguments(object?[] arguments, List<Func<object?, string?>> formatters)
    {
        if (arguments.Length == 0)
        {
            return string.Empty;
        }

        var formattedArgs = arguments.Select(arg => ArgumentFormatter.Format(arg, formatters)).ToArray();
        return string.Join(", ", formattedArgs);
    }

    /// <summary>
    /// Formats an array of arguments using default formatting
    /// </summary>
    public static string FormatArguments(object?[] arguments)
    {
        if (arguments.Length == 0)
        {
            return string.Empty;
        }

        var formattedArgs = arguments.Select(arg => ArgumentFormatter.Format(arg, [
        ])).ToArray();
        return string.Join(", ", formattedArgs);
    }

    /// <summary>
    /// Creates a display name from test metadata and arguments
    /// </summary>
    public static string CreateDisplayName(TestMetadata metadata, object?[] arguments, TestDataCombination? dataCombination = null)
    {
        // If we have a custom display name from data combination, use it
        if (!string.IsNullOrEmpty(dataCombination?.DisplayName))
        {
            return dataCombination!.DisplayName!;
        }

        // Otherwise create default display name
        var testName = metadata.TestName;

        if (arguments.Length == 0)
        {
            return testName;
        }

        var argumentsText = FormatArguments(arguments);
        return $"{testName}({argumentsText})";
    }

    /// <summary>
    /// Creates a display name with generic type information
    /// </summary>
    public static string CreateGenericDisplayName(TestMetadata metadata, Type[] genericTypes, object?[] arguments)
    {
        var testName = metadata.TestName;

        if (genericTypes.Length > 0)
        {
            var genericPart = string.Join(", ", genericTypes.Select(GetSimpleTypeName));
            testName = $"{testName}<{genericPart}>";
        }

        if (arguments.Length == 0)
        {
            return testName;
        }

        var argumentsText = FormatArguments(arguments);
        return $"{testName}({argumentsText})";
    }

    private static string GetSimpleTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.GetGenericTypeDefinition().Name;
        var index = genericTypeName.IndexOf('`');
        if (index > 0)
        {
            genericTypeName = genericTypeName.Substring(0, index);
        }

        var genericArgs = type.GetGenericArguments();
        var genericArgsText = string.Join(", ", genericArgs.Select(GetSimpleTypeName));

        return $"{genericTypeName}<{genericArgsText}>";
    }

}
