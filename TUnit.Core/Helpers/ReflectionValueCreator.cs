using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

[SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
internal static class ReflectionValueCreator
{

    public static async Task<object?> CreatePropertyValueAsync(TestClass classInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        IDataAttribute generator,
        TestProperty property,
        DataGeneratorMetadata dataGeneratorMetadata) =>
        generator switch
        {
            ArgumentsAttribute argumentsAttribute => argumentsAttribute.Values.ElementAtOrDefault(0),
            ClassConstructorAttribute classConstructorAttribute => ((IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!).Create(
                property.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContextAccessor.Current,
                    TestSessionId = string.Empty
                }),
            IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute => await GetFirstAsyncValueWithInitAsync(asyncDataSourceGeneratorAttribute, dataGeneratorMetadata).ConfigureAwait(false),
            MethodDataSourceAttribute methodDataSourceAttribute => await InvokeMethodDataSourceAsync(methodDataSourceAttribute, classInformation.Type).ConfigureAwait(false),
            NoOpDataAttribute => null,
            _ => throw new ArgumentOutOfRangeException(nameof(generator), generator, null)
        };


    private static async Task<object?> GetFirstAsyncValueWithInitAsync(IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return await GetFirstAsyncValueAsync(asyncDataSourceGeneratorAttribute.GenerateAsync(dataGeneratorMetadata)).ConfigureAwait(false);
    }

    private static async Task<object?> GetFirstAsyncValueAsync(IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable)
    {
        var enumerator = asyncEnumerable.GetAsyncEnumerator();
        try
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var func = enumerator.Current;
                var task = func();
                var result = await task.ConfigureAwait(false);
                return result?.ElementAtOrDefault(0);
            }
            return null;
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<object?> InvokeMethodDataSourceAsync(MethodDataSourceAttribute methodDataSourceAttribute, Type classType)
    {
        var methodDataSourceType = methodDataSourceAttribute.ClassProvidingDataSource ?? classType;

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Static | BindingFlags.FlattenHierarchy;

        var method = methodDataSourceType.GetMethod(
            name: methodDataSourceAttribute.MethodNameProvidingDataSource,
            bindingAttr: bindingFlags,
            binder: null,
            types: methodDataSourceAttribute.Arguments.Select(x => x?.GetType() ?? typeof(object)).ToArray(),
            modifiers: null)!;

        var result = method.Invoke(null, methodDataSourceAttribute.Arguments);

        // If the method returns a Task or ValueTask, we need to unwrap it
        if (IsAsyncResult(result))
        {
            return await UnwrapAsyncResultAsync(result).ConfigureAwait(false);
        }

        // For collections, return the first element
        if (result is not string and IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().FirstOrDefault();
        }

        return result;
    }

    private static bool IsAsyncResult(object? result)
    {
        if (result is null)
            return false;

        var type = result.GetType();
        return typeof(Task).IsAssignableFrom(type) || type.Name.StartsWith("ValueTask");
    }

    private static async Task<object?> UnwrapAsyncResultAsync(object? result)
    {
        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            return null;
        }

        // Handle ValueTask
        var valueTaskType = result!.GetType();
        if (valueTaskType.IsGenericType)
        {
            // Get the AsTask method and convert to Task
            var asTaskMethod = valueTaskType.GetMethod("AsTask");
            var convertedTask = (Task)asTaskMethod!.Invoke(result, null)!;

            await convertedTask.ConfigureAwait(false);

            var taskType = convertedTask.GetType();
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(convertedTask);
        }
        else
        {
            // Non-generic ValueTask
            var asTaskMethod = valueTaskType.GetMethod("AsTask");
            var convertedTask = (Task)asTaskMethod!.Invoke(result, null)!;

            await convertedTask.ConfigureAwait(false);

            return null;
        }
    }
}
