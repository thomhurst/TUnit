using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.ReferenceTracking;

namespace TUnit.Core;

/// <summary>
/// Unified data source processor that handles all data source types consistently
/// for both AOT and reflection modes.
/// </summary>
public static class DataSourceProcessor
{
    /// <summary>
    /// Processes a data source result and converts it into an array of value factories.
    /// Handles IEnumerable, arrays, tuples, Func<T>, and single values.
    /// </summary>
    public static Func<Task<object?>>[] ProcessDataSource(
        object? dataSourceResult,
        int expectedParameterCount)
    {
        if (dataSourceResult == null)
        {
            return CreateSingleFactory(null);
        }

        // Handle Func<T> by invoking it
        var unwrappedResult = UnwrapFuncResult(dataSourceResult);

        // Handle different data source result types
        if (IsTuple(unwrappedResult) && unwrappedResult != null)
        {
            return ProcessTuple(unwrappedResult);
        }

        if (unwrappedResult is IEnumerable enumerable && !IsString(enumerable))
        {
            return ProcessEnumerable(enumerable, expectedParameterCount);
        }

        return ProcessSingleValue(unwrappedResult, expectedParameterCount);
    }

    /// <summary>
    /// Resolves a data source value, handling Func<T> invocation and tuple unpacking.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Data source processing requires reflection on runtime types")]
    public static async Task<object?> ResolveDataSourceValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        // Handle Func<T>
        var unwrapped = UnwrapFuncResult(value);

        // Handle async Func
        if (unwrapped is Task task)
        {
            await task;
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = GetResultProperty(taskType);
                unwrapped = resultProperty?.GetValue(task);
            }
            else
            {
                unwrapped = null;
            }
        }

        // Track the resolved object
        return DataSourceReferenceTrackerProvider.TrackDataSourceObject(unwrapped);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Data source processing requires reflection on runtime types")]
    private static object? UnwrapFuncResult(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var invokeMethod = GetInvokeMethod(type);
            return invokeMethod?.Invoke(value, null);
        }

        return value;
    }

    private static bool IsTuple(object? value)
    {
        if (value == null)
        {
            return false;
        }

        var type = value.GetType();
        return type is { IsGenericType: true, FullName: not null } &&
            type.FullName.StartsWith("System.ValueTuple");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Data source processing requires reflection on runtime types")]
    private static Func<Task<object?>>[] ProcessTuple(object tuple)
    {
        var tupleType = tuple.GetType();
        var fields = GetTupleFields(tupleType);
        var factories = new Func<Task<object?>>[fields.Length];

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var item = field.GetValue(tuple);
            factories[i] = async () => await ResolveDataSourceValue(item);
        }

        return factories;
    }

    private static Func<Task<object?>>[] ProcessEnumerable(IEnumerable enumerable, int expectedParameterCount)
    {
        var items = enumerable.Cast<object?>().ToList();

        // If the enumerable has the expected number of items, treat each as a parameter
        if (items.Count == expectedParameterCount && expectedParameterCount > 0)
        {
            return items.Select(item =>
                new Func<Task<object?>>(async () => await ResolveDataSourceValue(item))
            ).ToArray();
        }

        // Otherwise, treat the entire collection as a single parameter
        return CreateSingleFactory(items.ToArray());
    }

    private static Func<Task<object?>>[] ProcessSingleValue(object? value, int expectedParameterCount)
    {
        // If we expect multiple parameters but got a single value,
        // check if it's an array or collection
        if (expectedParameterCount > 1 && value is IEnumerable enumerable && !IsString(enumerable))
        {
            return ProcessEnumerable(enumerable, expectedParameterCount);
        }

        // Single value for single parameter or fallback
        return CreateSingleFactory(value);
    }

    private static Func<Task<object?>>[] CreateSingleFactory(object? value)
    {
        return [async () => await ResolveDataSourceValue(value)];
    }

    private static bool IsString(object obj)
    {
        return obj is string;
    }

    /// <summary>
    /// Creates a factory function that invokes a data source method with proper error handling.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Data source processing requires reflection on runtime types")]
    public static Func<Task<object?[]>> CreateDataSourceInvoker(
        Func<object?> dataSourceMethod,
        int expectedParameterCount)
    {
        return async () =>
        {
            try
            {
                var result = dataSourceMethod();

                // Handle async results
                if (result is Task task)
                {
                    await task;
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        var resultProperty = GetResultProperty(taskType);
                        result = resultProperty?.GetValue(task);
                    }
                    else
                    {
                        result = null;
                    }
                }

                // Process the result into parameter factories
                var factories = ProcessDataSource(result, expectedParameterCount);

                // Resolve all factories to get actual values
                var values = new object?[factories.Length];
                for (int i = 0; i < factories.Length; i++)
                {
                    values[i] = await factories[i]();
                }

                return values;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke data source: {ex.Message}", ex);
            }
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Data source processing requires reflection access")]
    private static PropertyInfo? GetResultProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type taskType)
    {
        return taskType.GetProperty("Result");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Data source processing requires reflection access")]
    private static MethodInfo? GetInvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        return type.GetMethod("Invoke");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Data source processing requires reflection access")]
    private static FieldInfo[] GetTupleFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type tupleType)
    {
        return tupleType.GetFields();
    }
}
