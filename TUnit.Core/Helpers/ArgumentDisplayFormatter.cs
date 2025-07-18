using System.Collections;
using System.Text;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides consistent formatting of test arguments for display across both 
/// source generation and reflection modes.
/// </summary>
public static class ArgumentDisplayFormatter
{
    /// <summary>
    /// Formats a collection of arguments into a display string.
    /// </summary>
    public static string FormatArguments(IEnumerable<object?> arguments)
    {
        return string.Join(", ", arguments.Select(FormatSingleArgument));
    }

    /// <summary>
    /// Formats a single argument value for display.
    /// </summary>
    public static string FormatSingleArgument(object? arg)
    {
        if (arg is null)
        {
            return "null";
        }

        // Handle arrays and collections by showing their elements
        if (arg is IEnumerable enumerable && arg is not string)
        {
            return FormatEnumerable(enumerable);
        }

        // Handle primitive types
        var type = arg.GetType();
        if (type.IsPrimitive)
        {
            return arg.ToString()!;
        }

        // Handle strings with quotes
        if (arg is string str)
        {
            // Don't quote if it looks like it's already been formatted
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                return str;
            }
            // For empty strings, show them clearly
            if (string.IsNullOrEmpty(str))
            {
                return "\"\"";
            }
            return str;
        }

        // Handle enums
        if (arg is Enum)
        {
            return arg.ToString()!;
        }

        // Handle types
        if (arg is Type typeArg)
        {
            return FormatType(typeArg);
        }

        // For other types, check if ToString is overridden
        var toString = arg.ToString()!;
        if (toString == type.FullName || toString == type.AssemblyQualifiedName)
        {
            // ToString not overridden, return just the type name
            return type.Name;
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
            elements.Add(FormatSingleArgument(element));
            count++;
        }

        return string.Join(", ", elements);
    }

    /// <summary>
    /// Formats a Type for display.
    /// </summary>
    private static string FormatType(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var genericArgs = type.GetGenericArguments().Select(FormatType);
            return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
        }

        // Use simple name for common types
        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Boolean" => "bool",
            "Double" => "double",
            "Single" => "float",
            "Decimal" => "decimal",
            "Object" => "object",
            _ => type.Name
        };
    }

    /// <summary>
    /// Generates source code for formatting arguments at compile time.
    /// Used by source generators to emit consistent formatting code.
    /// </summary>
    public static string GenerateFormattingCode(string argumentExpression)
    {
        return $"TUnit.Core.Helpers.ArgumentDisplayFormatter.FormatSingleArgument({argumentExpression})";
    }

    /// <summary>
    /// Generates source code for formatting a collection of arguments.
    /// </summary>
    public static string GenerateFormattingCodeForCollection(string argumentsExpression)
    {
        return $"TUnit.Core.Helpers.ArgumentDisplayFormatter.FormatArguments({argumentsExpression})";
    }
}