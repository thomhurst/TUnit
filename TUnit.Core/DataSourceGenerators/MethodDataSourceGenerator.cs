using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for MethodDataSourceAttribute.
/// Invokes the specified method to get data and converts results to combinations.
/// </summary>
public class MethodDataSourceGenerator : IDataSourceGenerator<MethodDataSourceAttribute>
{
    public async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(MethodDataSourceAttribute attribute, DataSourceGenerationContext context)
    {
        await Task.Yield(); // Make it properly async
        
        var sourceType = attribute.ClassProvidingDataSource ?? context.TestClassType;
        var method = GetMethodFromType(sourceType, attribute.MethodNameProvidingDataSource, 
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        if (method == null)
        {
            throw new InvalidOperationException(
                $"Method '{attribute.MethodNameProvidingDataSource}' not found on type '{sourceType.Name}'");
        }

        // Invoke method once to determine structure and count
        var result = InvokeDataSourceMethod(method, sourceType, attribute.Arguments);
        
        if (result is IEnumerable<object?[]> objectArrays)
        {
            var loopIndex = 0;
            foreach (var objectArray in objectArrays)
            {
                var currentLoopIndex = loopIndex; // Capture for closure
                yield return new TestDataCombination
                {
                    MethodDataFactories = objectArray.Select<object?, Func<Task<object?>>>((_, paramIndex) => 
                        () => Task.FromResult(GetMethodDataAtIndex(method, sourceType, attribute.Arguments, currentLoopIndex, paramIndex))).ToArray(),
                    ClassDataFactories = [
                    ],
                    MethodDataSourceIndex = context.DataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                };
                loopIndex++;
            }
        }
        else if (result is System.Collections.IEnumerable enumerable)
        {
            var loopIndex = 0;
            foreach (var item in enumerable)
            {
                var currentLoopIndex = loopIndex; // Capture for closure
                object?[] methodData;
                if (item is object?[] array)
                {
                    methodData = array;
                }
                else
                {
                    methodData = [item];
                }

                yield return new TestDataCombination
                {
                    MethodDataFactories = methodData.Select<object?, Func<Task<object?>>>((_, paramIndex) => 
                        () => Task.FromResult(GetMethodDataAtIndex(method, sourceType, attribute.Arguments, currentLoopIndex, paramIndex))).ToArray(),
                    ClassDataFactories = [
                    ],
                    MethodDataSourceIndex = context.DataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                };
                loopIndex++;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Method '{attribute.MethodNameProvidingDataSource}' on type '{sourceType.Name}' " +
                "must return an IEnumerable of data values");
        }
    }

    private static object? InvokeDataSourceMethod(MethodInfo method, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourceType, object?[]? arguments)
    {
        object? instance = null;
        if (!method.IsStatic)
        {
            instance = Activator.CreateInstance(sourceType);
        }

        return method.Invoke(instance, arguments);
    }

    private static object? GetMethodDataAtIndex(MethodInfo method, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourceType, object?[]? arguments, int loopIndex, int paramIndex)
    {
        var result = InvokeDataSourceMethod(method, sourceType, arguments);
        
        if (result is IEnumerable<object?[]> objectArrays)
        {
            var currentLoop = 0;
            foreach (var objectArray in objectArrays)
            {
                if (currentLoop == loopIndex)
                {
                    return paramIndex < objectArray.Length ? objectArray[paramIndex] : null;
                }
                currentLoop++;
            }
        }
        else if (result is System.Collections.IEnumerable enumerable)
        {
            var currentLoop = 0;
            foreach (var item in enumerable)
            {
                if (currentLoop == loopIndex)
                {
                    object?[] methodData;
                    if (item is object?[] array)
                    {
                        methodData = array;
                    }
                    else
                    {
                        methodData = [item];
                    }
                    return paramIndex < methodData.Length ? methodData[paramIndex] : null;
                }
                currentLoop++;
            }
        }
        
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Method resolution required for data source invocation")]
    private static MethodInfo? GetMethodFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type sourceType, string methodName, BindingFlags bindingFlags)
    {
        return sourceType.GetMethod(methodName, bindingFlags);
    }
}