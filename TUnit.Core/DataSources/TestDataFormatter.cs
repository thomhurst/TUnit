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

        var formattedArgs = new string[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            formattedArgs[i] = ArgumentFormatter.Format(arguments[i], formatters);
        }
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

        var formattedArgs = new string[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            formattedArgs[i] = ArgumentFormatter.Format(arguments[i], []);
        }
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
            var genericTypeNames = new string[genericTypes.Length];
            for (var i = 0; i < genericTypes.Length; i++)
            {
                genericTypeNames[i] = GetSimpleTypeName(genericTypes[i]);
            }
            var genericPart = string.Join(", ", genericTypeNames);
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
        var genericArgNames = new string[genericArgs.Length];
        for (var i = 0; i < genericArgs.Length; i++)
        {
            genericArgNames[i] = GetSimpleTypeName(genericArgs[i]);
        }
        var genericArgsText = string.Join(", ", genericArgNames);

        return $"{genericTypeName}<{genericArgsText}>";
    }

}
