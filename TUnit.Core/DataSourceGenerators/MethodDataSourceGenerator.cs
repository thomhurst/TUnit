using System.Reflection;
using TUnit.Core;

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
        var method = sourceType.GetMethod(attribute.MethodNameProvidingDataSource, 
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        if (method == null)
        {
            throw new InvalidOperationException(
                $"Method '{attribute.MethodNameProvidingDataSource}' not found on type '{sourceType.Name}'");
        }

        object? instance = null;
        if (!method.IsStatic)
        {
            instance = Activator.CreateInstance(sourceType);
        }

        var result = method.Invoke(instance, attribute.Arguments);
        
        if (result is IEnumerable<object?[]> objectArrays)
        {
            var loopIndex = 0;
            foreach (var objectArray in objectArrays)
            {
                yield return new TestDataCombination
                {
                    MethodDataFactories = objectArray.Select<object?, Func<object?>>(item => () => item).ToArray(),
                    ClassDataFactories = Array.Empty<Func<object?>>(),
                    MethodDataSourceIndex = context.DataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                    PropertyValueFactories = new Dictionary<string, Func<object?>>()
                };
                loopIndex++;
            }
        }
        else if (result is System.Collections.IEnumerable enumerable)
        {
            var loopIndex = 0;
            foreach (var item in enumerable)
            {
                object?[] methodData;
                if (item is object?[] array)
                {
                    methodData = array;
                }
                else
                {
                    methodData = new[] { item };
                }

                yield return new TestDataCombination
                {
                    MethodDataFactories = methodData.Select<object?, Func<object?>>(item => () => item).ToArray(),
                    ClassDataFactories = Array.Empty<Func<object?>>(),
                    MethodDataSourceIndex = context.DataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                    PropertyValueFactories = new Dictionary<string, Func<object?>>()
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
}