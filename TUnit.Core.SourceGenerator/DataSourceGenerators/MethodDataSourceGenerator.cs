using System.Reflection;
using TUnit.Core;

namespace TUnit.Core.SourceGenerator.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for MethodDataSourceAttribute.
/// Invokes the specified method to get data and converts results to combinations.
/// </summary>
public class MethodDataSourceGenerator : IDataSourceGenerator<MethodDataSourceAttribute>
{
    public IEnumerable<TestDataCombination> GenerateDataCombinations(MethodDataSourceAttribute attribute, DataSourceGenerationContext context)
    {
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
            var index = 0;
            foreach (var objectArray in objectArrays)
            {
                yield return new TestDataCombination
                {
                    MethodData = objectArray,
                    ClassData = Array.Empty<object?>(),
                    DataSourceIndices = new[] { context.DataSourceIndex, index++ },
                    PropertyValues = new Dictionary<string, object?>()
                };
            }
        }
        else if (result is IEnumerable enumerable)
        {
            var index = 0;
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
                    MethodData = methodData,
                    ClassData = Array.Empty<object?>(),
                    DataSourceIndices = new[] { context.DataSourceIndex, index++ },
                    PropertyValues = new Dictionary<string, object?>()
                };
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