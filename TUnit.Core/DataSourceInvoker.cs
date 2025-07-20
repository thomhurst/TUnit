using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

public static class DataSourceInvoker
{
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> InvokeDataSourceAsync(
        IDataSourceAttribute dataSourceAttribute,
        DataGeneratorMetadata metadata)
    {
        IAsyncEnumerable<Func<Task<object?[]?>>> dataRows;
        
        try
        {
            dataRows = dataSourceAttribute.GetDataRowsAsync(metadata);
        }
        catch (Exception ex)
        {
            throw new DataGenerationException(
                $"Failed to get data rows from {dataSourceAttribute.GetType().Name}",
                dataSourceAttribute,
                ex);
        }

        if (dataRows == null)
        {
            throw new DataGenerationException(
                $"Data source {dataSourceAttribute.GetType().Name} returned null",
                dataSourceAttribute);
        }

        await foreach (var dataRow in dataRows)
        {
            if (dataRow == null)
            {
                throw new DataGenerationException(
                    $"Data source {dataSourceAttribute.GetType().Name} yielded a null data row function",
                    dataSourceAttribute);
            }

            yield return dataRow;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection-based data source invocation is expected in non-AOT scenarios")]
    public static async Task<object?> InvokeMethodAsync(
        object? instance,
        MethodInfo method,
        object?[]? parameters = null)
    {
        try
        {
            var result = method.Invoke(instance, parameters ?? [
            ]);
            
            if (result is Task task)
            {
                await task;
                
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
                
                return null;
            }

            return result;
        }
        catch (TargetInvocationException ex)
        {
            throw new DataGenerationException(
                $"Failed to invoke method {method.Name}",
                method,
                ex.InnerException ?? ex);
        }
        catch (Exception ex)
        {
            throw new DataGenerationException(
                $"Failed to invoke method {method.Name}",
                method,
                ex);
        }
    }

    public static async IAsyncEnumerable<object?> EnumerateDataAsync(object? data)
    {
        if (data == null)
        {
            yield return null;
            yield break;
        }

        if (data is IAsyncEnumerable<object?> asyncEnumerable)
        {
            await foreach (var item in asyncEnumerable)
            {
                yield return item;
            }
        }
        else if (data is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }
        }
        else
        {
            yield return data;
        }
    }

    public static object?[]? ConvertToArgumentArray(object? data, int expectedParameterCount)
    {
        if (data == null)
        {
            return null;
        }

        if (data is object?[] array)
        {
            if (array.Length != expectedParameterCount)
            {
                throw new DataGenerationException(
                    $"Data array has {array.Length} elements but method expects {expectedParameterCount} parameters",
                    data);
            }
            return array;
        }

        if (data.GetType().IsTupleType())
        {
            return ConvertTupleToArray(data);
        }

        if (expectedParameterCount == 1)
        {
            return [data];
        }

        throw new DataGenerationException(
            $"Cannot convert data of type {data.GetType()} to parameter array. Expected {expectedParameterCount} parameters.",
            data);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection-based tuple conversion is expected in non-AOT scenarios")]
    private static object?[]? ConvertTupleToArray(object tuple)
    {
        var tupleType = tuple.GetType();
        var fields = tupleType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name.StartsWith("Item"))
            .OrderBy(f => f.Name)
            .ToArray();

        var values = new object?[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            values[i] = fields[i].GetValue(tuple);
        }

        return values;
    }

    private static bool IsTupleType(this Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(ValueTuple<>) ||
               genericTypeDefinition == typeof(ValueTuple<,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,,,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,,,,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,,,,,>) ||
               genericTypeDefinition == typeof(ValueTuple<,,,,,,,>) ||
               genericTypeDefinition == typeof(Tuple<>) ||
               genericTypeDefinition == typeof(Tuple<,>) ||
               genericTypeDefinition == typeof(Tuple<,,>) ||
               genericTypeDefinition == typeof(Tuple<,,,>) ||
               genericTypeDefinition == typeof(Tuple<,,,,>) ||
               genericTypeDefinition == typeof(Tuple<,,,,,>) ||
               genericTypeDefinition == typeof(Tuple<,,,,,,>) ||
               genericTypeDefinition == typeof(Tuple<,,,,,,,>);
    }
}