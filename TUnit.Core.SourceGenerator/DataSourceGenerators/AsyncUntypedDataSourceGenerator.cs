using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Core.SourceGenerator.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for AsyncUntypedDataSourceGeneratorAttribute.
/// This generator handles untyped async data sources and requires dynamic code generation.
/// </summary>
[RequiresDynamicCode("Untyped async data sources require dynamic code generation")]
public class AsyncUntypedDataSourceGenerator : IDataSourceGenerator<AsyncUntypedDataSourceGeneratorAttribute>
{
    public IEnumerable<TestDataCombination> GenerateDataCombinations(AsyncUntypedDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
    {
        var asyncEnumerable = GetAsyncEnumerable(attribute, context);
        return ConvertAsyncEnumerableToDataCombinations(asyncEnumerable, context.DataSourceIndex);
    }

    private static object GetAsyncEnumerable(AsyncUntypedDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
    {
        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            TestClassType = context.TestClassType,
            TestMethodName = context.TestMethodName,
            ParameterTypes = context.ParameterTypes
        };

        return attribute.GenerateAsync(dataGeneratorMetadata);
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

                    if (factoryResult is Task<object?[]?> task)
                    {
                        var methodData = task.GetAwaiter().GetResult() ?? Array.Empty<object?>();

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