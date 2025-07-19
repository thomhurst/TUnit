using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test metadata implementation that uses reflection for legacy/discovery scenarios
/// </summary>
internal sealed class ReflectionTestMetadata : TestMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _testClass;
    private readonly MethodInfo _testMethod;
    private Func<IAsyncEnumerable<TestDataCombination>>? _dataCombinationGenerator;
    private Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest>? _createExecutableTestFactory;

    public ReflectionTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        MethodInfo testMethod)
    {
        _testClass = testClass;
        _testMethod = testMethod;
    }

    public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator
    {
        get
        {
            if (_dataCombinationGenerator == null)
            {
                _dataCombinationGenerator = GenerateDataCombinations;
            }
            return _dataCombinationGenerator;
        }
    }

    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            if (_createExecutableTestFactory == null)
            {
                _createExecutableTestFactory = CreateExecutableTest;
            }
            return _createExecutableTestFactory;
        }
    }

    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinations()
    {
        // Wrap the entire data generation in error handling
        await foreach (var combination in DataCombinationBuilder.BuildCombinationsWithErrorHandlingAsync(GenerateDataCombinationsCore))
        {
            yield return combination;
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsCore()
    #pragma warning restore CS1998
    {
        // Extract data sources from attributes using reflection
        var methodDataSources = ExtractMethodDataSources();
        var classDataSources = ExtractClassDataSources();
        var propertyDataSources = ExtractPropertyDataSources();

        // Get repeat count from method, class, or assembly level
        var repeatCount = GetRepeatCount();

        // If no data sources and no repeat, yield a single empty combination
        if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
        {
            for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
            {
                yield return new TestDataCombination
                {
                    MethodDataFactories = Array.Empty<Func<Task<object?>>>(),
                    ClassDataFactories = Array.Empty<Func<Task<object?>>>(),
                    PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),
                    MethodDataSourceIndex = -1,
                    MethodLoopIndex = 0,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                    RepeatIndex = repeatIndex
                };
            }
            yield break;
        }

        // Generate all combinations without awaiting inside the async enumerable
        var methodDataCombinations = await ProcessDataSourcesAsync(methodDataSources, ProcessMethodDataSourceAsync);
        var classDataCombinations = await ProcessDataSourcesAsync(classDataSources, ProcessClassDataSourceAsync);
        var propertyDataCombinations = GeneratePropertyDataCombinations(propertyDataSources);

        // Use the unified DataCombinationBuilder
        await foreach (var combination in DataCombinationBuilder.BuildCombinationsAsync(
            methodDataCombinations,
            classDataCombinations,
            propertyDataCombinations,
            repeatCount))
        {
            yield return combination;
        }
    }

    private List<T> ProcessDataSourcesWithErrorHandling<T>(List<TestDataSource> sources, Func<TestDataSource, IEnumerable<T>> processor)
        where T : new()
    {
        var results = new List<T>();
        
        foreach (var source in sources)
        {
            try
            {
                results.AddRange(processor(source));
            }
            catch (Exception ex)
            {
                // Instead of logging and continuing, create an error combination
                // This will be caught by the outer error handler and converted to TestDataCombination with DataGenerationException
                throw new Exception($"Failed to process data source: {ex.Message}", ex);
            }
        }
        
        return results;
    }

    private async Task<List<T>> ProcessDataSourcesAsync<T>(List<TestDataSource> sources, Func<TestDataSource, Task<IEnumerable<T>>> processor)
        where T : new()
    {
        var results = new List<T>();
        
        foreach (var source in sources)
        {
            try
            {
                var items = await processor(source);
                results.AddRange(items);
            }
            catch (Exception ex)
            {
                // Instead of logging and continuing, create an error combination
                // This will be caught by the outer error handler and converted to TestDataCombination with DataGenerationException
                throw new Exception($"Failed to process data source: {ex.Message}", ex);
            }
        }
        
        return results;
    }

    private ExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Create instance factory that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<TestContext, Task<object>> createInstance = async (testContext) =>
        {
            #pragma warning restore CS1998
            if (InstanceFactory == null)
            {
                throw new InvalidOperationException($"No instance factory for {_testClass.Name}");
            }

            var instance = InstanceFactory(context.ClassArguments);

            // Apply property values using unified PropertyInjector
            await PropertyInjector.InjectPropertiesAsync(
                instance, 
                context.PropertyValues, 
                metadata.PropertyInjections);

            return instance;
        };

        // Create test invoker with CancellationToken support
        // Determine if the test method has a CancellationToken parameter
        var hasCancellationToken = ParameterTypes.Any(t => t == typeof(CancellationToken));
        var cancellationTokenIndex = hasCancellationToken 
            ? Array.IndexOf(ParameterTypes, typeof(CancellationToken))
            : -1;

        Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
        {
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }

            if (hasCancellationToken)
            {
                // Insert CancellationToken at the correct position
                var argsWithToken = new object?[args.Length + 1];
                var argIndex = 0;
                
                for (int i = 0; i < argsWithToken.Length; i++)
                {
                    if (i == cancellationTokenIndex)
                    {
                        argsWithToken[i] = cancellationToken;
                    }
                    else if (argIndex < args.Length)
                    {
                        argsWithToken[i] = args[argIndex++];
                    }
                }
                
                await TestInvoker(instance, argsWithToken);
            }
            else
            {
                await TestInvoker(instance, args);
            }
        };

        return new UnifiedExecutableTest(createInstance, invokeTest)
        {
            TestId = context.TestId,
            DisplayName = context.DisplayName,
            Metadata = metadata,
            Arguments = context.Arguments,
            ClassArguments = context.ClassArguments,
            PropertyValues = context.PropertyValues,
            BeforeTestHooks = context.BeforeTestHooks,
            AfterTestHooks = context.AfterTestHooks,
            Context = context.Context
        };
    }

    private List<TestDataSource> ExtractMethodDataSources()
    {
        // Use the data sources already extracted by ReflectionTestDataCollector
        // This includes custom data source generators like MatrixDataSourceAttribute
        if (DataSources != null && DataSources.Length > 0)
        {
            return DataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var sources = new List<TestDataSource>();

        var attributes = _testMethod.GetCustomAttributes().ToList();

        // Process Arguments attributes
        foreach (var attr in attributes.OfType<ArgumentsAttribute>())
        {
            sources.Add(new StaticTestDataSource(attr.Values));
        }

        // Process MethodDataSource attributes
        var methodDataAttrs = attributes.OfType<MethodDataSourceAttribute>().ToList();
        foreach (var attr in methodDataAttrs)
        {
            try
            {
                var dataSource = CreateMethodDataSource(attr);
                if (dataSource != null)
                {
                    sources.Add(dataSource);
                }
            }
            catch (Exception ex)
            {
                // Method data source failures are configuration errors that should fail the test
                throw new InvalidOperationException($"Failed to create method data source for {attr.MethodNameProvidingDataSource}: {ex.Message}", ex);
            }
        }

        return sources;
    }

    private List<TestDataSource> ExtractClassDataSources()
    {
        // Use the data sources already extracted by ReflectionTestDataCollector
        if (ClassDataSources != null && ClassDataSources.Length > 0)
        {
            return ClassDataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var sources = new List<TestDataSource>();

        var attributes = _testClass.GetCustomAttributes().ToList();

        // Process Arguments attributes on the class
        foreach (var attr in attributes.OfType<ArgumentsAttribute>())
        {
            sources.Add(new StaticTestDataSource(attr.Values));
        }

        // Process MethodDataSource attributes on the class
        foreach (var attr in attributes.OfType<MethodDataSourceAttribute>())
        {
            try
            {
                var dataSource = CreateMethodDataSource(attr);
                if (dataSource != null)
                {
                    sources.Add(dataSource);
                }
            }
            catch (Exception ex)
            {
                // Class method data source failures are configuration errors that should fail the test
                throw new InvalidOperationException($"Failed to create class method data source for {attr.MethodNameProvidingDataSource}: {ex.Message}", ex);
            }
        }

        // Process ClassDataSource attributes
        foreach (var attr in attributes.OfType<ClassDataSourceAttribute>())
        {
            try
            {
                var dataSource = CreateClassDataSource(attr);
                if (dataSource != null)
                {
                    sources.Add(dataSource);
                }
            }
            catch (Exception ex)
            {
                // Class data source failure will be wrapped in TestDataCombination by error handling
                throw new Exception($"Failed to create class data source: {ex.Message}", ex);
            }
        }

        return sources;
    }

    private List<PropertyDataSource> ExtractPropertyDataSources()
    {
        // Use the property data sources already extracted by ReflectionTestDataCollector
        if (PropertyDataSources != null && PropertyDataSources.Length > 0)
        {
            return PropertyDataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var sources = new List<PropertyDataSource>();

        var properties = _testClass.GetProperties()
            .Where(p => p.GetCustomAttributes().Any(a => a is ArgumentsAttribute || a is MethodDataSourceAttribute || a is ClassDataSourceAttribute))
            .ToList();

        foreach (var property in properties)
        {
            var attributes = property.GetCustomAttributes().ToList();

            // Process Arguments attributes on properties
            foreach (var attr in attributes.OfType<ArgumentsAttribute>())
            {
                sources.Add(new PropertyDataSource
                {
                    PropertyName = property.Name,
                    PropertyType = property.PropertyType,
                    DataSource = new StaticTestDataSource(attr.Values)
                });
            }

            // Process MethodDataSource attributes on properties
            foreach (var attr in attributes.OfType<MethodDataSourceAttribute>())
            {
                try
                {
                    var dataSource = CreateMethodDataSource(attr);
                    if (dataSource != null)
                    {
                        sources.Add(new PropertyDataSource
                        {
                            PropertyName = property.Name,
                            PropertyType = property.PropertyType,
                            DataSource = dataSource
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Property data source failure will be wrapped in TestDataCombination by error handling
                    throw new Exception($"Failed to create property method data source for {property.Name}: {ex.Message}", ex);
                }
            }
        }

        return sources;
    }

    private static bool IsDataSourceAttribute(Type attributeType)
    {
        return attributeType.Name.EndsWith("DataAttribute") ||
               attributeType.Name.EndsWith("DataSourceAttribute") ||
               attributeType.Name == "ArgumentsAttribute";
    }

    private int GetRepeatCount()
    {
        // Check method level first
        var methodRepeat = _testMethod.GetCustomAttribute<RepeatAttribute>();
        if (methodRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return methodRepeat.Times + 1;
        }

        // Check class level
        var classRepeat = _testClass.GetCustomAttribute<RepeatAttribute>();
        if (classRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return classRepeat.Times + 1;
        }

        // Check assembly level
        var assemblyRepeat = _testClass.Assembly.GetCustomAttribute<RepeatAttribute>();
        if (assemblyRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return assemblyRepeat.Times + 1;
        }

        return 1; // Default to 1 if no repeat attribute found
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    [UnconditionalSuppressMessage("Trimming", "IL2080:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    private TestDataSource? CreateMethodDataSource(MethodDataSourceAttribute attr)
    {
        var targetType = attr.ClassProvidingDataSource ?? _testClass;
        var method = targetType.GetMethod(attr.MethodNameProvidingDataSource,
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null)
        {
            throw new InvalidOperationException($"Method {attr.MethodNameProvidingDataSource} not found on type {targetType.Name}");
        }

        // Create a delegate data source that invokes the method
        return new DelegateDataSource(() =>
        {
            try
            {
                object? instance = null;
                if (!method.IsStatic)
                {
                    instance = Activator.CreateInstance(targetType);
                }

                var result = method.Invoke(instance, attr.Arguments);

                // Handle different return types
                if (result is IEnumerable<object?[]> enumerable)
                {
                    var items = enumerable.ToList();
                    return items;
                }
                if (result is IEnumerable<object> objects && !(result is string))
                {
                    var items = objects.Select(obj => new[] { obj }).ToList();
                    return items;
                }
                if (result is object[] array)
                {
                    return new[] { array };
                }
                return new[] { new[] { result } };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invoke method {attr.MethodNameProvidingDataSource}: {ex.Message}", ex);
            }
        });
    }

    [UnconditionalSuppressMessage("Trimming", "IL2062:The parameter of method has a DynamicallyAccessedMembersAttribute, but the value passed to it can not be statically analyzed.", Justification = "This is reflection mode where dynamic type access is expected")]
    private TestDataSource? CreateClassDataSource(ClassDataSourceAttribute attr)
    {
        try
        {
            // Use reflection to get the _types field from the attribute
            var typesField = typeof(ClassDataSourceAttribute).GetField("_types", BindingFlags.NonPublic | BindingFlags.Instance);
            if (typesField?.GetValue(attr) is not Type[] types || types.Length == 0)
            {
                throw new InvalidOperationException("ClassDataSourceAttribute has no types configured");
            }

            // Create a delegate data source that creates instances of each type
            return new DelegateDataSource(() =>
            {
                try
                {
                    var items = new object?[types.Length];

                    for (var i = 0; i < types.Length; i++)
                    {
                        // Create instance of the type
                        var instance = Activator.CreateInstance(types[i]);
                        items[i] = instance;
                    }

                    return new[] { items };
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create class data source instances: {ex.Message}", ex);
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create class data source: {ex.Message}", ex);
        }
    }

    private List<MethodDataCombination> ProcessMethodDataSource(TestDataSource dataSource)
    {
        var combinations = new List<MethodDataCombination>();
        var factories = dataSource.GetDataFactories();
        int loopIndex = 0;

        foreach (var factory in factories)
        {
            var data = factory();
            
            // Handle the case where data is object?[] instead of object[]
            IEnumerable<object?> dataEnumerable;
            if (data is object?[] nullableArray)
            {
                dataEnumerable = nullableArray;
            }
            else
            {
                dataEnumerable = data;
            }
            
            var dataFactories = dataEnumerable.Select(value => new Func<Task<object?>>(async () =>
            {
                var resolvedValue = await ResolveTestDataValueAsync(value);
                return resolvedValue;
            })).ToArray();
            
            combinations.Add(new MethodDataCombination
            {
                DataFactories = dataFactories,
                DataSourceIndex = 0,
                LoopIndex = loopIndex++
            });
        }

        return combinations;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection mode cannot support AOT")]
    private static async Task<object?> ResolveTestDataValueAsync(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<Task<T>>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var returnType = type.GetGenericArguments()[0];
            
            // Invoke the Func to get the result
            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod!.Invoke(value, null);
            
            // If the result is a Task, await it
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                
                // Get the Result property for Task<T>
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
                
                // For non-generic Task
                return null;
            }
            
            return result;
        }

        // Check if it's already a Task<T>
        if (value is Task task2)
        {
            await task2.ConfigureAwait(false);
            
            var taskType = task2.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task2);
            }
            
            return null;
        }

        // Check for other delegate types that might need invocation
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                // It's a parameterless delegate, invoke it
                var result = invokeMethod.Invoke(value, null);
                
                // Recursively resolve in case it returns a Task
                return await ResolveTestDataValueAsync(result).ConfigureAwait(false);
            }
        }

        return value;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection mode cannot support AOT")]
    private static object? ResolveTestDataValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<T> (has Invoke method with no parameters and returns something)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            // Invoke the Func<T> to get the actual value
            return type.GetMethod("Invoke")!.Invoke(value, null);
        }

        // Check for other delegate types that might need invocation
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                // It's a parameterless delegate, invoke it
                return invokeMethod.Invoke(value, null);
            }
        }

        return value;
    }

    private async Task<IEnumerable<MethodDataCombination>> ProcessMethodDataSourceAsync(TestDataSource dataSource)
    {
        var combinations = new List<MethodDataCombination>();
        int loopIndex = 0;

        // Check if it's an async data source
        if (dataSource is AsyncTestDataSource asyncDataSource)
        {
            await foreach (var factory in asyncDataSource.GetDataFactoriesAsync())
            {
                var data = factory();
                
                // Handle the case where data is object?[] instead of object[]
                IEnumerable<object?> dataEnumerable;
                if (data is object?[] nullableArray)
                {
                    dataEnumerable = nullableArray;
                }
                else
                {
                    dataEnumerable = data;
                }
                
                var dataFactories = dataEnumerable.Select(value => new Func<Task<object?>>(async () =>
                {
                    var resolvedValue = await ResolveTestDataValueAsync(value);
                    return resolvedValue;
                })).ToArray();
                
                combinations.Add(new MethodDataCombination
                {
                    DataFactories = dataFactories,
                    DataSourceIndex = 0,
                    LoopIndex = loopIndex++
                });
            }
        }
        else
        {
            // Fall back to synchronous processing for non-async data sources
            var syncCombinations = ProcessMethodDataSource(dataSource);
            combinations.AddRange(syncCombinations);
        }

        return combinations;
    }

    private List<ClassDataCombination> ProcessClassDataSource(TestDataSource dataSource)
    {
        var combinations = new List<ClassDataCombination>();
        var factories = dataSource.GetDataFactories();
        int loopIndex = 0;

        foreach (var factory in factories)
        {
            var data = factory();
            var dataFactories = data.Select(value => new Func<Task<object?>>(async () =>
            {
                var resolvedValue = await ResolveTestDataValueAsync(value);
                return resolvedValue;
            })).ToArray();

            combinations.Add(new ClassDataCombination
            {
                DataFactories = dataFactories,
                DataSourceIndex = 0,
                LoopIndex = loopIndex++
            });
        }

        return combinations;
    }

    private async Task<IEnumerable<ClassDataCombination>> ProcessClassDataSourceAsync(TestDataSource dataSource)
    {
        var combinations = new List<ClassDataCombination>();
        int loopIndex = 0;

        // Check if it's an async data source
        if (dataSource is AsyncTestDataSource asyncDataSource)
        {
            await foreach (var factory in asyncDataSource.GetDataFactoriesAsync())
            {
                var data = factory();
                var dataFactories = data.Select(value => new Func<Task<object?>>(async () =>
                {
                    var resolvedValue = await ResolveTestDataValueAsync(value);
                    return resolvedValue;
                })).ToArray();

                combinations.Add(new ClassDataCombination
                {
                    DataFactories = dataFactories,
                    DataSourceIndex = 0,
                    LoopIndex = loopIndex++
                });
            }
        }
        else
        {
            // Fall back to synchronous processing for non-async data sources
            var syncCombinations = ProcessClassDataSource(dataSource);
            combinations.AddRange(syncCombinations);
        }

        return combinations;
    }

    private List<PropertyDataCombination> GeneratePropertyDataCombinations(List<PropertyDataSource> propertyDataSources)
    {
        var combinations = new List<PropertyDataCombination>();

        if (!propertyDataSources.Any())
        {
            return combinations;
        }

        var propertyValueFactories = new Dictionary<string, Func<Task<object?>>>();

        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                // Get data synchronously for property data sources
                var factories = propertyDataSource.DataSource.GetDataFactories();
                var firstFactory = factories.FirstOrDefault();
                if (firstFactory != null)
                {
                    var data = firstFactory();
                    if (data != null && data.Length > 0)
                    {
                        var value = data[0];
                        propertyValueFactories[propertyDataSource.PropertyName] = () => Task.FromResult(value);
                    }
                }
            }
            catch (Exception ex)
            {
                // Property data generation failure is a configuration error that should fail the test
                throw new InvalidOperationException($"Failed to generate property data for {propertyDataSource.PropertyName}: {ex.Message}", ex);
            }
        }

        combinations.Add(new PropertyDataCombination
        {
            PropertyValueFactories = propertyValueFactories
        });

        return combinations;
    }
}
