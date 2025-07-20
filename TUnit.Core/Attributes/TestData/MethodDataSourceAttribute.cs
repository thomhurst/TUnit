using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute(typeof(T), methodNameProvidingDataSource);

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute : TestDataAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public object?[] Arguments { get; set; } = [];

    public MethodDataSourceAttribute(string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    public MethodDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type classProvidingDataSource,
        string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        ClassProvidingDataSource = classProvidingDataSource ?? throw new ArgumentNullException(nameof(classProvidingDataSource), "No class type was provided");
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:UnrecognizedReflectionPattern", Justification = "Data source methods use dynamic patterns")]
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Data source methods use dynamic patterns")]
    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var targetType = ClassProvidingDataSource ?? dataGeneratorMetadata.TestClassType;
        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;

        var methodInfo = targetType.GetMethod(MethodNameProvidingDataSource, bindingFlags)
            ?? throw new InvalidOperationException($"Method '{MethodNameProvidingDataSource}' not found in class '{targetType.Name}'.");

        // Determine if it's an instance method
        object? instance = null;
        if (!methodInfo.IsStatic)
        {
            instance = dataGeneratorMetadata.TestClassInstance ?? Activator.CreateInstance(targetType);
        }

        var methodResult = methodInfo.Invoke(instance, Arguments);

        // Handle different return types
        if (methodResult == null)
        {
            yield break;
        }

        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(methodResult.GetType()))
        {
            await foreach (var item in ConvertToAsyncEnumerable(methodResult))
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
            }
        }
        // If it's Task<IEnumerable>
        else if (methodResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);

            if (taskResult is System.Collections.IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
                }
            }
            else
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(taskResult));
            }
        }
        // Regular IEnumerable
        else if (methodResult is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(methodResult));
        }
    }

    private static bool IsAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Data source methods may use dynamic patterns")]
    private static async IAsyncEnumerable<object?> ConvertToAsyncEnumerable(object asyncEnumerable)
    {
        var type = asyncEnumerable.GetType();
        var enumeratorMethod = type.GetMethod("GetAsyncEnumerator");
        var enumerator = enumeratorMethod!.Invoke(asyncEnumerable, [CancellationToken.None]);

        var moveNextMethod = enumerator!.GetType().GetMethod("MoveNextAsync");
        var currentProperty = enumerator.GetType().GetProperty("Current");

        while (true)
        {
            var moveNextTask = (ValueTask<bool>)moveNextMethod!.Invoke(enumerator, null)!;
            if (!await moveNextTask.ConfigureAwait(false))
            {
                break;
            }

            yield return currentProperty!.GetValue(enumerator);
        }

        // Dispose the enumerator
        var disposeMethod = enumerator.GetType().GetMethod("DisposeAsync");
        if (disposeMethod != null)
        {
            var disposeTask = (ValueTask)disposeMethod.Invoke(enumerator, null)!;
            await disposeTask.ConfigureAwait(false);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Task result property access")]
    private static object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();

        if (taskType.IsGenericType)
        {
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return null;
    }

    private static object?[] ConvertToObjectArray(object? item)
    {
        if (item == null)
        {
            return [null];
        }

        if (item is object?[] objArray)
        {
            return objArray;
        }

        if (item.GetType().IsArray)
        {
            var array = (Array)item;
            var result = new object?[array.Length];
            array.CopyTo(result, 0);
            return result;
        }

        return [item];
    }
}
