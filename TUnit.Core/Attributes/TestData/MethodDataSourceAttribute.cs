using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute(typeof(T), methodNameProvidingDataSource);

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Static
        | System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.FlattenHierarchy;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public Func<DataGeneratorMetadata, IAsyncEnumerable<Func<Task<object?[]?>>>>? Factory { get; set; }

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
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (Factory != null)
        {
            await foreach (var func in Factory(dataGeneratorMetadata))
            {
                yield return func;
            }

            yield break;
        }

        if (dataGeneratorMetadata.MembersToGenerate.Length == 0)
        {
            throw new InvalidOperationException($"No members to generate were provided for {dataGeneratorMetadata.Type}");
        }

        var item1 = dataGeneratorMetadata.MembersToGenerate[0];

        var targetType = ClassProvidingDataSource
            ?? (item1 as PropertyMetadata)?.ClassMetadata.Type
            ?? TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);

        // If we have a test class instance and no explicit class was provided,
        // use the instance's actual type (which will be the constructed generic type)
        if (ClassProvidingDataSource == null && dataGeneratorMetadata.TestClassInstance != null)
        {
            targetType = dataGeneratorMetadata.TestClassInstance.GetType();
        }
        
        if (targetType == null)
        {
            throw new InvalidOperationException($"Could not determine target type for method '{MethodNameProvidingDataSource}'. This may occur during static property initialization without a test context.");
        }

        var methodInfo = targetType.GetMethods(BindingFlags).SingleOrDefault(x => x.Name == MethodNameProvidingDataSource
                && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(Arguments.Select(a => a?.GetType())))
            ?? targetType.GetMethod(MethodNameProvidingDataSource, BindingFlags)
            ?? throw new InvalidOperationException(
                $"Method '{MethodNameProvidingDataSource}' not found in class '{targetType.Name}' with the specified arguments.");

        if (methodInfo is null)
        {
            throw new InvalidOperationException($"Method '{MethodNameProvidingDataSource}' not found in class '{targetType.Name}'.");
        }

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
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(item.ToObjectArray());
                };
            }
        }
        // If it's Task<IEnumerable>
        else if (methodResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);

            if (taskResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(taskResult))
            {
                foreach (var item in enumerable)
                {
                    yield return async () =>
                    {
                        return await Task.FromResult<object?[]?>(item.ToObjectArray());
                    };
                }
            }
            else
            {
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(taskResult.ToObjectArray());
                };
            }
        }
        // Regular IEnumerable - but check if it's a tuple first
        // Tuples implement IEnumerable but should be treated as single values
        else if (methodResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(methodResult))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(item.ToObjectArray());
            }
        }
        else
        {
            yield return async () =>
            {
                return await Task.FromResult<object?[]?>(methodResult.ToObjectArray());
            };
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
}
