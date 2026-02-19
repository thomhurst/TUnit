using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Provides test data from a method, property, or field on the specified type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type containing the data source member.</typeparam>
/// <param name="methodNameProvidingDataSource">The name of the method, property, or field that provides the test data.</param>
/// <example>
/// <code>
/// [Test]
/// [MethodDataSource&lt;TestDataProvider&gt;(nameof(TestDataProvider.GetTestCases))]
/// public void MyTest(int input, string expected) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class MethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute(typeof(T), methodNameProvidingDataSource);

/// <summary>
/// Provides test data from a method, property, or field in the test class or a specified type.
/// </summary>
/// <remarks>
/// <para>
/// The data source can be a method, property, or field that returns test data.
/// Supported return types include single values, <see cref="IEnumerable{T}"/>, <see cref="IAsyncEnumerable{T}"/>,
/// <see cref="Task{T}"/>, tuples, and arrays.
/// </para>
/// <para>
/// When no class type is specified, the data source is looked up in the test class itself.
/// Both static and instance members are supported.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a method in the same class
/// [Test]
/// [MethodDataSource(nameof(GetTestData))]
/// public void MyTest(int value, string name) { }
///
/// public static IEnumerable&lt;(int, string)&gt; GetTestData()
/// {
///     yield return (1, "one");
///     yield return (2, "two");
/// }
///
/// // Using a method in another class
/// [Test]
/// [MethodDataSource(typeof(SharedData), nameof(SharedData.GetValues))]
/// public void MyTest(string value) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class MethodDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Static
        | System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.FlattenHierarchy;

    /// <summary>
    /// Gets the type containing the data source member, or <c>null</c> if the data source is in the test class itself.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type? ClassProvidingDataSource { get; }

    /// <summary>
    /// Gets the name of the method, property, or field that provides the test data.
    /// </summary>
    public string MethodNameProvidingDataSource { get; }

    /// <summary>
    /// Gets or sets an AOT-safe factory function for providing test data programmatically.
    /// When set, this factory is used instead of reflection-based member lookup.
    /// </summary>
    public Func<DataGeneratorMetadata, IAsyncEnumerable<Func<Task<object?[]?>>>>? Factory { get; set; }

    /// <summary>
    /// Gets or sets the arguments to pass to the data source method.
    /// Use this when the data source method requires parameters.
    /// </summary>
    public object?[] Arguments { get; set; } = [];

    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

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
        // Skip PlaceholderInstance as it's used during discovery when the actual instance isn't created yet
        if (ClassProvidingDataSource == null
            && dataGeneratorMetadata.TestClassInstance != null
            && dataGeneratorMetadata.TestClassInstance is not PlaceholderInstance)
        {
            targetType = dataGeneratorMetadata.TestClassInstance.GetType();
        }

        // If the target type is abstract or interface, we can't create an instance of it.
        // Fall back to the test class type which should be concrete.
        // BUT: Don't override if ClassProvidingDataSource was explicitly provided, even if it's a static class
        // (static classes are abstract in IL but contain static members we can invoke)
        if (ClassProvidingDataSource == null && targetType != null && (targetType.IsAbstract || targetType.IsInterface))
        {
            var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
            if (testClassType != null && !testClassType.IsAbstract && !testClassType.IsInterface)
            {
                targetType = testClassType;
            }
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
                instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
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
                    instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
                }

                methodResult = propertyInfo.GetValue(instance);
            }
            else if (fieldInfo != null)
            {
                // Determine if it's an instance field
                object? instance = null;
                if (!fieldInfo.IsStatic)
                {
                    instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
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

        // Compute paramTypes once to avoid repeated LINQ allocations
        var paramTypes = GetParameterTypes(dataGeneratorMetadata.TestInformation?.Parameters);

        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(methodResult.GetType()))
        {
            var hasAnyItems = false;
            await foreach (var item in ConvertToAsyncEnumerable(methodResult))
            {
                hasAnyItems = true;
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                };
            }

            // If the async enumerable was empty, yield one empty result like NoDataSource does
            // unless SkipIfEmpty is true, in which case we yield nothing (test will be skipped)
            if (!hasAnyItems && !SkipIfEmpty)
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
                        return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                    };
                }

                // If the enumerable was empty, yield one empty result like NoDataSource does
                // unless SkipIfEmpty is true, in which case we yield nothing (test will be skipped)
                if (!hasAnyItems && !SkipIfEmpty)
                {
                    yield return () => Task.FromResult<object?[]?>([]);
                }
            }
            else
            {
                yield return async () =>
                {
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
                yield return () => Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
            }

            // If the enumerable was empty, yield one empty result like NoDataSource does
            // unless SkipIfEmpty is true, in which case we yield nothing (test will be skipped)
            if (!hasAnyItems && !SkipIfEmpty)
            {
                yield return () => Task.FromResult<object?[]?>([]);
            }
        }
        else
        {
            yield return async () =>
            {
                return await Task.FromResult<object?[]?>(methodResult.ToObjectArrayWithTypes(paramTypes));
            };
        }
    }

    private static Type[]? GetParameterTypes(ParameterMetadata[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return null;
        }

        var types = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            types[i] = parameters[i].Type;
        }
        return types;
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

    /// <summary>
    /// Gets an existing test class instance or creates a new one.
    /// Uses InstanceFactory if available (which can perform property injection),
    /// otherwise falls back to Activator.CreateInstance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
    private static async Task<object?> GetOrCreateInstanceAsync(DataGeneratorMetadata metadata, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType)
    {
        // First check if we have a valid test class instance
        var testClassInstance = metadata.TestClassInstance;
        if (testClassInstance is PlaceholderInstance)
        {
            testClassInstance = null;
        }

        if (testClassInstance != null)
        {
            return testClassInstance;
        }

        // Try to use the InstanceFactory if available (which can perform property injection)
        if (metadata.InstanceFactory != null)
        {
            return await metadata.InstanceFactory(targetType);
        }

        // Fall back to creating a bare instance
        return Activator.CreateInstance(targetType);
    }
}
