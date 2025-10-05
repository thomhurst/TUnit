using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
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

        // Try to find a method first
        var methodInfo = targetType.GetMethods(BindingFlags).SingleOrDefault(x => x.Name == MethodNameProvidingDataSource
                && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(Arguments.Select(a => a?.GetType())))
            ?? targetType.GetMethod(MethodNameProvidingDataSource, BindingFlags);

        object? methodResult;

        if (methodInfo != null)
        {
            // Determine if it's an instance method
            object? instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = dataGeneratorMetadata.TestClassInstance ?? Activator.CreateInstance(targetType);
            }

            methodResult = methodInfo.Invoke(instance, Arguments);
        }
        else
        {
            // Try to find a property or field
            var propertyInfo = targetType.GetProperty(MethodNameProvidingDataSource, BindingFlags);
            var fieldInfo = targetType.GetField(MethodNameProvidingDataSource, BindingFlags);

            if (propertyInfo != null)
            {
                // Determine if it's an instance property
                object? instance = null;
                if (propertyInfo.GetMethod?.IsStatic != true)
                {
                    instance = dataGeneratorMetadata.TestClassInstance ?? Activator.CreateInstance(targetType);
                }

                methodResult = propertyInfo.GetValue(instance);
            }
            else if (fieldInfo != null)
            {
                // Determine if it's an instance field
                object? instance = null;
                if (!fieldInfo.IsStatic)
                {
                    instance = dataGeneratorMetadata.TestClassInstance ?? Activator.CreateInstance(targetType);
                }

                methodResult = fieldInfo.GetValue(instance);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Method, property, or field '{MethodNameProvidingDataSource}' not found in class '{targetType.Name}' with the specified arguments.");
            }
        }

        // Handle different return types
        if (methodResult == null)
        {
            yield break;
        }

        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(methodResult.GetType()))
        {
            var hasAnyItems = false;
            await foreach (var item in ConvertToAsyncEnumerable(methodResult))
            {
                hasAnyItems = true;
                yield return async () =>
                {
                    var paramTypes = dataGeneratorMetadata.TestInformation?.Parameters.Select(p => p.Type).ToArray();
                    return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                };
            }
            
            // If the async enumerable was empty, yield one empty result like NoDataSource does
            if (!hasAnyItems)
            {
                yield return () => Task.FromResult<object?[]?>([]);
            }
        }
        // If it's Task<IEnumerable>
        else if (methodResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);

            if (taskResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(taskResult))
            {
                var hasAnyItems = false;
                foreach (var item in enumerable)
                {
                    hasAnyItems = true;
                    yield return async () =>
                    {
                        var paramTypes = dataGeneratorMetadata.TestInformation?.Parameters.Select(p => p.Type).ToArray();
                        return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                    };
                }
                
                // If the enumerable was empty, yield one empty result like NoDataSource does
                if (!hasAnyItems)
                {
                    yield return () => Task.FromResult<object?[]?>([]);
                }
            }
            else
            {
                yield return async () =>
                {
                    var paramTypes = dataGeneratorMetadata.TestInformation?.Parameters.Select(p => p.Type).ToArray();
                    return await Task.FromResult<object?[]?>(taskResult.ToObjectArrayWithTypes(paramTypes));
                };
            }
        }
        // Regular IEnumerable - but check if it's a tuple first
        // Tuples implement IEnumerable but should be treated as single values
        else if (methodResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(methodResult))
        {
            var hasAnyItems = false;
            foreach (var item in enumerable)
            {
                hasAnyItems = true;
                var paramTypes = dataGeneratorMetadata.TestInformation?.Parameters.Select(p => p.Type).ToArray();
                yield return () => Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
            }
            
            // If the enumerable was empty, yield one empty result like NoDataSource does
            if (!hasAnyItems)
            {
                yield return () => Task.FromResult<object?[]?>([]);
            }
        }
        else
        {
            var paramTypes = dataGeneratorMetadata.TestInformation?.Parameters.Select(p => p.Type).ToArray();
            yield return async () =>
            {
                return await Task.FromResult<object?[]?>(methodResult.ToObjectArrayWithTypes(paramTypes));
            };
        }
    }

    private static bool IsAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
    private static async IAsyncEnumerable<object?> ConvertToAsyncEnumerable(object asyncEnumerable, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var type = asyncEnumerable.GetType();
        
        // Find the IAsyncEnumerable<T> interface
        var asyncEnumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        
        if (asyncEnumerableInterface is null)
        {
            throw new InvalidOperationException($"Type {type.Name} does not implement IAsyncEnumerable<T>");
        }
        
        // Get the GetAsyncEnumerator method from the interface
        var enumeratorMethod = asyncEnumerableInterface.GetMethod("GetAsyncEnumerator");
        
        if (enumeratorMethod is null)
        {
            throw new InvalidOperationException($"Could not find GetAsyncEnumerator method on interface {asyncEnumerableInterface.Name}");
        }
        
        var enumerator = enumeratorMethod.Invoke(asyncEnumerable, [cancellationToken]);

        // The enumerator might not have MoveNextAsync directly on its type,
        // we need to look for it on the IAsyncEnumerator<T> interface
        var enumeratorType = enumerator!.GetType();
        
        // Find MoveNextAsync - first try the type directly, then check interfaces
        var moveNextMethod = enumeratorType.GetMethod("MoveNextAsync");
        if (moveNextMethod is null)
        {
            // Look for it on the IAsyncEnumerator<T> interface
            var asyncEnumeratorInterface = enumeratorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>));
            
            if (asyncEnumeratorInterface != null)
            {
                moveNextMethod = asyncEnumeratorInterface.GetMethod("MoveNextAsync");
            }
        }
        
        // Similarly for Current property
        var currentProperty = enumeratorType.GetProperty("Current");
        if (currentProperty is null)
        {
            // Look for it on the IAsyncEnumerator<T> interface
            var asyncEnumeratorInterface = enumeratorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>));
            
            if (asyncEnumeratorInterface != null)
            {
                currentProperty = asyncEnumeratorInterface.GetProperty("Current");
            }
        }

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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
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
