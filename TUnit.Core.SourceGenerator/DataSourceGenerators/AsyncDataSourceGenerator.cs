using System.Reflection;
using TUnit.Core;

namespace TUnit.Core.SourceGenerator.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for AsyncDataSourceGeneratorAttribute.
/// Handles async data sources by awaiting the async enumerable and converting results.
/// </summary>
public class AsyncDataSourceGenerator : IDataSourceGenerator<AsyncDataSourceGeneratorAttribute>
{
    public IEnumerable<TestDataCombination> GenerateDataCombinations(AsyncDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
    {
        var asyncEnumerable = GetAsyncEnumerable(attribute, context);
        return ConvertAsyncEnumerableToDataCombinations(asyncEnumerable, context.DataSourceIndex);
    }

    private static object GetAsyncEnumerable(AsyncDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
    {
        var attributeType = attribute.GetType();
        var generateMethod = attributeType.GetMethod("GenerateDataSourcesAsync", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (generateMethod == null)
        {
            throw new InvalidOperationException(
                $"GenerateDataSourcesAsync method not found on {attributeType.Name}");
        }

        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestClassType = context.TestClassType,
            TestMethodName = context.TestMethodName,
            ParameterTypes = context.ParameterTypes
        };

        var result = generateMethod.Invoke(attribute, new object[] { dataGeneratorMetadata });
        return result ?? throw new InvalidOperationException("GenerateDataSourcesAsync returned null");
    }

    private static IEnumerable<TestDataCombination> ConvertAsyncEnumerableToDataCombinations(object asyncEnumerable, int dataSourceIndex)
    {
        var enumerableType = asyncEnumerable.GetType();
        var getAsyncEnumeratorMethod = enumerableType.GetMethod("GetAsyncEnumerator");

        if (getAsyncEnumeratorMethod == null)
        {
            throw new InvalidOperationException("AsyncEnumerable does not have GetAsyncEnumerator method");
        }

        var enumerator = getAsyncEnumeratorMethod.Invoke(asyncEnumerable, new object[] { CancellationToken.None });
        var enumeratorType = enumerator!.GetType();
        var moveNextAsyncMethod = enumeratorType.GetMethod("MoveNextAsync");
        var currentProperty = enumeratorType.GetProperty("Current");

        if (moveNextAsyncMethod == null || currentProperty == null)
        {
            throw new InvalidOperationException("AsyncEnumerator is missing required methods/properties");
        }

        var index = 0;
        var combinations = new List<TestDataCombination>();

        try
        {
            while (true)
            {
                var moveNextTask = moveNextAsyncMethod.Invoke(enumerator, null) as Task<bool>;
                var hasNext = moveNextTask?.GetAwaiter().GetResult() ?? false;

                if (!hasNext)
                {
                    break;
                }

                var current = currentProperty.GetValue(enumerator);
                var factoryFunc = current as Delegate;

                if (factoryFunc != null)
                {
                    var invokeMethod = factoryFunc.GetType().GetMethod("Invoke");
                    var factoryResult = invokeMethod?.Invoke(factoryFunc, null);

                    if (factoryResult is Task task)
                    {
                        task.GetAwaiter().GetResult();
                        var resultProperty = task.GetType().GetProperty("Result");
                        var taskResult = resultProperty?.GetValue(task);

                        object?[] methodData;
                        if (taskResult is object?[] array)
                        {
                            methodData = array;
                        }
                        else
                        {
                            methodData = new[] { taskResult };
                        }

                        combinations.Add(new TestDataCombination
                        {
                            MethodData = methodData,
                            ClassData = Array.Empty<object?>(),
                            DataSourceIndices = new[] { dataSourceIndex, index++ },
                            PropertyValues = new Dictionary<string, object?>()
                        });
                    }
                }
            }
        }
        finally
        {
            if (enumerator is IAsyncDisposable asyncDisposable)
            {
                var disposeTask = asyncDisposable.DisposeAsync();
                if (!disposeTask.IsCompleted)
                {
                    disposeTask.AsTask().GetAwaiter().GetResult();
                }
            }
            else if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return combinations;
    }
}