using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Shared helper class for building display names consistently across both AOT and reflection modes
/// </summary>
internal static class DisplayNameBuilder
{
    /// <summary>
    /// Builds a display name for a test, checking for DisplayNameAttribute first
    /// </summary>
    public static string BuildDisplayName(
        TestMetadata metadata,
        object?[] arguments,
        TestDataCombination? dataCombination = null)
    {
        // First check if we have a custom display name from data combination
        if (dataCombination?.DisplayName != null)
        {
            return dataCombination.DisplayName;
        }

        // Check for DisplayNameAttribute in metadata
        // Note: DisplayNameAttribute is processed through discovery event receivers
        // so we just return the default display name here
        return BuildDefaultDisplayName(metadata, arguments);
    }

    /// <summary>
    /// Builds the default display name format: TestName(arg1, arg2, ...)
    /// </summary>
    public static string BuildDefaultDisplayName(TestMetadata metadata, object?[] arguments)
    {
        var testName = metadata.TestName;
        
        if (arguments.Length == 0)
        {
            return testName;
        }

        var argumentsText = FormatArguments(arguments);
        return $"{testName}({argumentsText})";
    }

    /// <summary>
    /// Formats test arguments for display using consistent formatting
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
    /// Formats test arguments with custom formatters
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
    /// Builds display name with generic type information
    /// </summary>
    public static string BuildGenericDisplayName(TestMetadata metadata, Type[] genericTypes, object?[] arguments)
    {
        var testName = metadata.TestName;
        
        if (genericTypes.Length > 0)
        {
            var genericPart = string.Join(", ", genericTypes.Select(t => GetSimpleTypeName(t)));
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

    /// <summary>
    /// Resolves the actual value from a data source factory result
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2075:Target method return value does not satisfy annotation requirements.",
        Justification = "This is for reflection mode which doesn't support AOT")]
    public static async Task<object?> ResolveDataSourceValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Task<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)value;
            await task.ConfigureAwait(false);
            
            var resultProperty = type.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        // Check if it's a ValueTask<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            // Convert to Task and await
            var asTaskMethod = type.GetMethod("AsTask");
            if (asTaskMethod != null)
            {
                var task = (Task?)asTaskMethod.Invoke(value, null);
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
            }
        }

        // Check if it's a Func<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var invokeMethod = type.GetMethod("Invoke");
            return invokeMethod?.Invoke(value, null);
        }

        // Check if it's a Func<Task<T>>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>) 
            && type.GetGenericArguments()[0].IsGenericType 
            && type.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Task<>))
        {
            var invokeMethod = type.GetMethod("Invoke");
            var taskResult = invokeMethod?.Invoke(value, null);
            if (taskResult is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
        }

        return value;
    }
}