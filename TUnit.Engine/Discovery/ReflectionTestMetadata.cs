using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;

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

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinations()
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
        var methodDataCombinations = new List<MethodDataCombination>();
        var classDataCombinations = new List<ClassDataCombination>();

        // Process method data sources synchronously
        foreach (var source in methodDataSources)
        {
            methodDataCombinations.AddRange(ProcessMethodDataSource(source));
        }

        // Process class data sources synchronously
        foreach (var source in classDataSources)
        {
            classDataCombinations.AddRange(ProcessClassDataSource(source));
        }

        var propertyDataCombinations = GeneratePropertyDataCombinations(propertyDataSources);

        // Generate cartesian product of all combinations
        var methodCombinations = methodDataCombinations.Any() ? methodDataCombinations.ToArray() : new[] { new MethodDataCombination() };
        var classCombinations = classDataCombinations.Any() ? classDataCombinations.ToArray() : new[] { new ClassDataCombination() };
        var propertyCombinations = propertyDataCombinations.Any() ? propertyDataCombinations.ToArray() : new[] { new PropertyDataCombination() };

        foreach (var methodCombination in methodCombinations)
        {
            foreach (var classCombination in classCombinations)
            {
                foreach (var propertyCombination in propertyCombinations)
                {
                    for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                    {
                        yield return new TestDataCombination
                        {
                            MethodDataFactories = methodCombination.DataFactories,
                            ClassDataFactories = classCombination.DataFactories,
                            PropertyValueFactories = propertyCombination.PropertyValueFactories,
                            MethodDataSourceIndex = methodCombination.DataSourceIndex,
                            MethodLoopIndex = methodCombination.LoopIndex,
                            ClassDataSourceIndex = classCombination.DataSourceIndex,
                            ClassLoopIndex = classCombination.LoopIndex,
                            RepeatIndex = repeatIndex
                        };
                    }
                }
            }
        }
    }

    private ExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Create instance factory that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<Task<object>> createInstance = async () =>
        {
            #pragma warning restore CS1998
            if (InstanceFactory == null)
            {
                throw new InvalidOperationException($"No instance factory for {_testClass.Name}");
            }

            var instance = InstanceFactory(context.ClassArguments);

            // Apply property values
            foreach (var kvp in context.PropertyValues)
            {
                var property = _testClass.GetProperty(kvp.Key);
                property?.SetValue(instance, kvp.Value);
            }

            return instance;
        };

        // Create test invoker that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<object, object?[], Task> invokeTest = async (instance, args) =>
        {
            #pragma warning restore CS1998
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }

            await TestInvoker(instance, args);
        };

        return new DynamicExecutableTest(createInstance, invokeTest)
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
        var sources = new List<TestDataSource>();

        var attributes = _testMethod.GetCustomAttributes().ToList();
        Console.WriteLine($"ExtractMethodDataSources: Found {attributes.Count} attributes on {_testMethod.Name}");

        // Process Arguments attributes
        foreach (var attr in attributes.OfType<ArgumentsAttribute>())
        {
            sources.Add(new StaticTestDataSource(attr.Values));
        }

        // Process MethodDataSource attributes
        var methodDataAttrs = attributes.OfType<MethodDataSourceAttribute>().ToList();
        Console.WriteLine($"ExtractMethodDataSources: Found {methodDataAttrs.Count} MethodDataSource attributes");
        foreach (var attr in methodDataAttrs)
        {
            try
            {
                Console.WriteLine($"ExtractMethodDataSources: Processing MethodDataSource for method {attr.MethodNameProvidingDataSource}");
                var dataSource = CreateMethodDataSource(attr);
                if (dataSource != null)
                {
                    sources.Add(dataSource);
                    Console.WriteLine($"ExtractMethodDataSources: Successfully created {dataSource.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to create method data source for {attr.MethodNameProvidingDataSource}: {ex.Message}");
            }
        }

        // Process DataSourceGenerator attributes (basic support for synchronous generators)
        foreach (var attr in attributes)
        {
            // Check if it's a DataSourceGeneratorAttribute<T> (synchronous version)
            if (attr.GetType().BaseType?.IsGenericType == true &&
                attr.GetType().BaseType?.GetGenericTypeDefinition().Name.Contains("DataSourceGeneratorAttribute") == true)
            {
                try
                {
                    var dataSource = CreateDataSourceGenerator(attr);
                    if (dataSource != null)
                    {
                        sources.Add(dataSource);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create data source generator: {ex.Message}");
                }
            }
            else if (attr is AsyncUntypedDataSourceGeneratorAttribute asyncUntypedAttr)
            {
                try
                {
                    var dataSource = CreateAsyncUntypedDataSourceGenerator(asyncUntypedAttr);
                    if (dataSource != null)
                    {
                        sources.Add(dataSource);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create async untyped data source generator: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else if (attr is IAsyncDataSourceGeneratorAttribute asyncAttr)
            {
                Console.WriteLine($"Warning: AsyncDataSourceGenerator attributes are not fully supported in reflection mode: {attr.GetType().Name}");
                // Skip async generators for now - they require complex metadata creation
            }
        }

        return sources;
    }

    private List<TestDataSource> ExtractClassDataSources()
    {
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
                Console.WriteLine($"Warning: Failed to create class method data source for {attr.MethodNameProvidingDataSource}: {ex.Message}");
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
                Console.WriteLine($"Warning: Failed to create class data source: {ex.Message}");
            }
        }

        return sources;
    }

    private List<PropertyDataSource> ExtractPropertyDataSources()
    {
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
                    Console.WriteLine($"Warning: Failed to create property method data source for {property.Name}: {ex.Message}");
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
            return methodRepeat.Times;
        }

        // Check class level
        var classRepeat = _testClass.GetCustomAttribute<RepeatAttribute>();
        if (classRepeat != null)
        {
            return classRepeat.Times;
        }

        // Check assembly level
        var assemblyRepeat = _testClass.Assembly.GetCustomAttribute<RepeatAttribute>();
        if (assemblyRepeat != null)
        {
            return assemblyRepeat.Times;
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
            Console.WriteLine($"Warning: Method {attr.MethodNameProvidingDataSource} not found on type {targetType.Name}");
            return null;
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
                Console.WriteLine($"CreateMethodDataSource: Method {method.Name} returned {result?.GetType()?.Name ?? "null"} with value {result}");

                // Handle different return types
                if (result is IEnumerable<object?[]> enumerable)
                {
                    var items = enumerable.ToList();
                    Console.WriteLine($"CreateMethodDataSource: Returning IEnumerable<object?[]> with {items.Count} items");
                    return items;
                }
                if (result is IEnumerable<object> objects && !(result is string))
                {
                    var items = objects.Select(obj => new[] { obj }).ToList();
                    Console.WriteLine($"CreateMethodDataSource: Returning IEnumerable<object> converted to {items.Count} items");
                    return items;
                }
                if (result is object[] array)
                {
                    Console.WriteLine($"CreateMethodDataSource: Returning object[] wrapped as single item");
                    return new[] { array };
                }
                Console.WriteLine($"CreateMethodDataSource: Returning single value wrapped as [[{result}]]");
                return new[] { new[] { result } };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to invoke method {attr.MethodNameProvidingDataSource}: {ex.Message}");
                return Enumerable.Empty<object?[]>();
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
                Console.WriteLine("Warning: ClassDataSourceAttribute has no types configured");
                return null;
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
                    Console.WriteLine($"Warning: Failed to create class data source instances: {ex.Message}");
                    return Enumerable.Empty<object?[]>();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to create class data source: {ex.Message}");
            return null;
        }
    }

    private List<MethodDataCombination> ProcessMethodDataSource(TestDataSource dataSource)
    {
        var combinations = new List<MethodDataCombination>();

        try
        {
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
                
                var dataFactories = dataEnumerable.Select(value => new Func<Task<object?>>(() =>
                {
                    var resolvedValue = ResolveTestDataValue(value);
                    return Task.FromResult(resolvedValue);
                })).ToArray();

                Console.WriteLine($"ProcessMethodDataSource: Created {dataFactories.Length} data factories from {dataSource.GetType().Name}");
                
                combinations.Add(new MethodDataCombination
                {
                    DataFactories = dataFactories,
                    DataSourceIndex = 0,
                    LoopIndex = loopIndex++
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process method data source: {ex.Message}");
        }

        return combinations;
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

    private List<ClassDataCombination> ProcessClassDataSource(TestDataSource dataSource)
    {
        var combinations = new List<ClassDataCombination>();

        try
        {
            var factories = dataSource.GetDataFactories();
            int loopIndex = 0;

            foreach (var factory in factories)
            {
                var data = factory();
                var dataFactories = data.Select(value => new Func<Task<object?>>(() =>
                {
                    var resolvedValue = ResolveTestDataValue(value);
                    return Task.FromResult(resolvedValue);
                })).ToArray();

                combinations.Add(new ClassDataCombination
                {
                    DataFactories = dataFactories,
                    DataSourceIndex = 0,
                    LoopIndex = loopIndex++
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process class data source: {ex.Message}");
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
                Console.WriteLine($"Warning: Failed to generate property data for {propertyDataSource.PropertyName}: {ex.Message}");
            }
        }

        combinations.Add(new PropertyDataCombination
        {
            PropertyValueFactories = propertyValueFactories
        });

        return combinations;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    private TestDataSource? CreateDataSourceGenerator(Attribute attr)
    {
        // Create a delegate data source that invokes the synchronous generator
        return new DelegateDataSource(() =>
        {
            try
            {
                // Initialize the generator attribute by setting its required properties
                // Note: We have to use GetAwaiter().GetResult() here because DelegateDataSource
                // expects a synchronous factory function. This is a limitation of the current
                // architecture where data sources are expected to be synchronous.
                var initializedGenerator = InitializeDataSourceGeneratorAsync(attr).GetAwaiter().GetResult();
                if (initializedGenerator == null)
                {
                    Console.WriteLine($"Warning: Failed to initialize data source generator {attr.GetType().Name}");
                    return Enumerable.Empty<object?[]>();
                }

                // Use reflection to call the GenerateDataSources method
                var generateMethod = initializedGenerator.GetType().GetMethod("GenerateDataSources",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (generateMethod == null)
                {
                    Console.WriteLine($"Warning: Could not find GenerateDataSources method on {initializedGenerator.GetType().Name}");
                    return Enumerable.Empty<object?[]>();
                }

                // Try to call the method with null metadata first (many generators don't use it)
                try
                {
                    var result = generateMethod.Invoke(initializedGenerator, new object[] { null! });
                    return ProcessGeneratorResult(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to call GenerateDataSources with null metadata: {ex.Message}");
                    return Enumerable.Empty<object?[]>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to generate data from data source generator: {ex.Message}");
                return Enumerable.Empty<object?[]>();
            }
        });
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic constructor access is expected")]
    private async Task<object?> InitializeDataSourceGeneratorAsync(Attribute attr)
    {
        try
        {
            // Create a new instance of the generator attribute type
            var generatorType = attr.GetType();
            var newGenerator = Activator.CreateInstance(generatorType);

            if (newGenerator == null)
            {
                Console.WriteLine($"Warning: Failed to create instance of {generatorType.Name}");
                return null;
            }

            // Copy properties from the original attribute to the new instance
            var properties = generatorType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;

                // Check if this property has a data source attribute
                var dataSourceAttrs = property.GetCustomAttributes().Where(a =>
                    a.GetType().Name.Contains("DataSource") ||
                    a.GetType().Name.Contains("ClassDataSource")).ToList();

                if (dataSourceAttrs.Any())
                {
                    // Initialize this property with an instance based on its data source attribute
                    var instance = await CreateInstanceForDataSourcePropertyAsync(property, dataSourceAttrs.First());
                    if (instance != null)
                    {
                        property.SetValue(newGenerator, instance);
                    }
                }
                else
                {
                    // Copy the value from the original attribute
                    var value = property.GetValue(attr);
                    if (value != null)
                    {
                        property.SetValue(newGenerator, value);
                    }
                }
            }

            return newGenerator;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize data source generator: {ex.Message}");
            return null;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic constructor access is expected")]
    private async Task<object?> CreateInstanceForDataSourcePropertyAsync(PropertyInfo property, Attribute dataSourceAttr)
    {
        try
        {
            // Get the target type from the property
            var targetType = property.PropertyType;

            // Create an instance of the target type
            var instance = Activator.CreateInstance(targetType);

            if (instance == null)
            {
                Console.WriteLine($"Warning: Failed to create instance of {targetType.Name}");
                return null;
            }

            // If the created instance has properties that need initialization, initialize them recursively
            await InitializeNestedPropertiesAsync(instance, targetType);

            // Initialize the entire object graph in the correct order (deepest first)
            await InitializeObjectGraphAsync(instance);

            // Note: InitializeAsync() is called by InitializeObjectGraphAsync in the correct order

            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to create instance for property {property.Name}: {ex.Message}");
            return null;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic property access is expected")]
    private async Task InitializeNestedPropertiesAsync(object instance, Type instanceType)
    {
        try
        {
            var properties = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;

                // Check if this property has a data source attribute
                var dataSourceAttrs = property.GetCustomAttributes().Where(a =>
                    a.GetType().Name.Contains("DataSource") ||
                    a.GetType().Name.Contains("ClassDataSource")).ToList();

                if (dataSourceAttrs.Any())
                {
                    // Initialize this property with an instance based on its data source attribute
                    var nestedInstance = await CreateInstanceForDataSourcePropertyAsync(property, dataSourceAttrs.First());
                    if (nestedInstance != null)
                    {
                        property.SetValue(instance, nestedInstance);
                        // Note: IAsyncInitializer.InitializeAsync() is already called in CreateInstanceForDataSourcePropertyAsync
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize nested properties for {instanceType.Name}: {ex.Message}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    private async Task InitializeObjectGraphAsync(object instance)
    {
        try
        {
            // Initialize children first (depth-first initialization)
            await InitializeChildrenAsync(instance);

            await ObjectInitializer.InitializeAsync(instance);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize object graph for {instance.GetType().Name}: {ex.Message}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    private async Task InitializeChildrenAsync(object instance)
    {
        try
        {
            var instanceType = instance.GetType();
            var properties = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.CanRead) continue;

                var value = property.GetValue(instance);
                if (value != null)
                {
                    // Check if this property has a data source attribute (indicating it's a nested instance we created)
                    var dataSourceAttrs = property.GetCustomAttributes().Where(a =>
                        a.GetType().Name.Contains("DataSource") ||
                        a.GetType().Name.Contains("ClassDataSource")).ToList();

                    if (dataSourceAttrs.Any())
                    {
                        // Recursively initialize this nested object
                        await InitializeObjectGraphAsync(value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize children for {instance.GetType().Name}: {ex.Message}");
        }
    }

    private IEnumerable<object?[]> ProcessGeneratorResult(object? result)
    {
        if (result is IEnumerable<Func<object>> funcs)
        {
            return funcs.Select(func => new[] { func() });
        }
        if (result is IEnumerable enumerable)
        {
            // Convert to object arrays
            var results = new List<object?[]>();
            foreach (var item in enumerable)
            {
                if (item is Func<object> func)
                {
                    results.Add(new[] { func() });
                }
                else
                {
                    results.Add(new[] { item });
                }
            }
            return results;
        }

        return Enumerable.Empty<object?[]>();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic method access is expected")]
    private TestDataSource? CreateAsyncUntypedDataSourceGenerator(AsyncUntypedDataSourceGeneratorAttribute attr)
    {
        // For AsyncUntypedDataSourceGeneratorAttribute (including UntypedDataSourceGeneratorAttribute like MatrixDataSource)
        return new DelegateDataSource(() =>
        {
            try
            {
                // Create DataGeneratorMetadata for the generator
                var metadata = CreateDataGeneratorMetadata();
                if (metadata == null)
                {
                    Console.WriteLine($"Warning: Failed to create metadata for {attr.GetType().Name}");
                    return Enumerable.Empty<object?[]>();
                }

                // Use reflection to call the GenerateDataSources method
                var generateMethod = attr.GetType().GetMethod("GenerateDataSources",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(DataGeneratorMetadata) },
                    null);

                if (generateMethod == null)
                {
                    Console.WriteLine($"Warning: Could not find GenerateDataSources method on {attr.GetType().Name}");
                    return Enumerable.Empty<object?[]>();
                }

                var result = generateMethod.Invoke(attr, new object[] { metadata });
                Console.WriteLine($"DEBUG: Generator returned result of type: {result?.GetType()?.FullName ?? "null"}");
                return ProcessUntypedGeneratorResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to generate data from async untyped data source generator: {ex.Message}");
                return Enumerable.Empty<object?[]>();
            }
        });
    }

    private IEnumerable<object?[]> ProcessUntypedGeneratorResult(object? result)
    {
        // Handle sync enumerable of funcs (which MatrixDataSource returns)
        if (result is IEnumerable<Func<object?[]?>> funcs)
        {
            Console.WriteLine($"DEBUG: Processing {funcs.Count()} function results");
            return funcs.Select(func => func() ?? Array.Empty<object?>()).Where(arr => arr != null);
        }
        if (result is IEnumerable enumerable)
        {
            // Convert to object arrays
            var results = new List<object?[]>();
            foreach (var item in enumerable)
            {
                if (item is Func<object?[]?> func)
                {
                    var arr = func();
                    if (arr != null)
                    {
                        results.Add(arr);
                    }
                }
                else if (item is object?[] arr)
                {
                    results.Add(arr);
                }
                else
                {
                    Console.WriteLine($"DEBUG: Unexpected item type in enumerable: {item?.GetType()?.FullName ?? "null"}");
                }
            }
            Console.WriteLine($"DEBUG: Processed {results.Count} results from enumerable");
            return results;
        }

        Console.WriteLine($"DEBUG: Result type not handled: {result?.GetType()?.FullName ?? "null"}");
        return Enumerable.Empty<object?[]>();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic type access is expected")]
    [UnconditionalSuppressMessage("Trimming", "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "This is reflection mode where dynamic type access is expected")]
    private DataGeneratorMetadata? CreateDataGeneratorMetadata()
    {
        try
        {
            // Create parameter metadata for the test method
            var parameters = _testMethod.GetParameters();
            var memberMetadata = new List<MemberMetadata>();

            foreach (var param in parameters)
            {
                memberMetadata.Add(new ParameterMetadata(param.ParameterType)
                {
                    Name = param.Name ?? string.Empty,
                    TypeReference = TypeReference.CreateConcrete(param.ParameterType.AssemblyQualifiedName ?? param.ParameterType.FullName ?? param.ParameterType.Name),
                    ReflectionInfo = param
                });
            }

            // Create assembly metadata
            var assemblyMetadata = new AssemblyMetadata
            {
                Name = _testClass.Assembly.GetName().Name ?? "Unknown"
            };

            // Get class properties
            var properties = _testClass.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(p => new PropertyMetadata
                {
                    Name = p.Name,
                    Type = p.PropertyType,
                    ReflectionInfo = p,
                    IsStatic = p.GetMethod?.IsStatic ?? false,
                    Getter = obj => p.GetValue(obj)
                })
                .ToArray();

            // Create class metadata
            var classMetadata = new ClassMetadata
            {
                Name = _testClass.Name,
                Type = _testClass,
                TypeReference = TypeReference.CreateConcrete(_testClass.AssemblyQualifiedName ?? _testClass.FullName ?? _testClass.Name),
                Namespace = _testClass.Namespace ?? string.Empty,
                Assembly = assemblyMetadata,
                Parameters = Array.Empty<ParameterMetadata>(), // No constructor parameters for reflection mode
                Properties = properties,
                Parent = _testClass.BaseType != null && _testClass.BaseType != typeof(object)
                    ? new ClassMetadata
                    {
                        Name = _testClass.BaseType.Name,
                        Type = _testClass.BaseType,
                        TypeReference = TypeReference.CreateConcrete(_testClass.BaseType.AssemblyQualifiedName ?? _testClass.BaseType.FullName ?? _testClass.BaseType.Name),
                        Namespace = _testClass.BaseType.Namespace ?? string.Empty,
                        Assembly = new AssemblyMetadata { Name = _testClass.BaseType.Assembly.GetName().Name ?? "Unknown" },
                        Parameters = Array.Empty<ParameterMetadata>(),
                        Properties = Array.Empty<PropertyMetadata>(),
                        Parent = null
                    }
                    : null
            };

            // Create method metadata
            var methodMetadata = new MethodMetadata
            {
                Name = _testMethod.Name,
                Type = _testMethod.DeclaringType ?? _testClass,
                Class = classMetadata,
                Parameters = memberMetadata.OfType<ParameterMetadata>().ToArray(),
                GenericTypeCount = _testMethod.IsGenericMethodDefinition ? _testMethod.GetGenericArguments().Length : 0,
                ReturnTypeReference = TypeReference.CreateConcrete(_testMethod.ReturnType.AssemblyQualifiedName ?? _testMethod.ReturnType.FullName ?? _testMethod.ReturnType.Name),
                ReturnType = _testMethod.ReturnType,
                TypeReference = TypeReference.CreateConcrete((_testMethod.DeclaringType ?? _testClass).AssemblyQualifiedName ?? (_testMethod.DeclaringType ?? _testClass).FullName ?? (_testMethod.DeclaringType ?? _testClass).Name)
            };

            return new DataGeneratorMetadata
            {
                TestInformation = methodMetadata,
                MembersToGenerate = memberMetadata.ToArray(),
                Type = DataGeneratorType.TestParameters,
                TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()), // Empty context for reflection mode
                TestSessionId = string.Empty, // No session ID in reflection mode
                TestClassInstance = null, // Will be set during test execution
                ClassInstanceArguments = null // Will be set during test execution
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to create DataGeneratorMetadata: {ex.Message}");
            return null;
        }
    }

}

// Helper classes for data combinations
internal class MethodDataCombination
{
    public Func<Task<object?>>[] DataFactories { get; set; } = Array.Empty<Func<Task<object?>>>();
    public int DataSourceIndex { get; set; } = -1;
    public int LoopIndex { get; set; }
}

internal class ClassDataCombination
{
    public Func<Task<object?>>[] DataFactories { get; set; } = Array.Empty<Func<Task<object?>>>();
    public int DataSourceIndex { get; set; } = -1;
    public int LoopIndex { get; set; }
}

internal class PropertyDataCombination
{
    public Dictionary<string, Func<Task<object?>>> PropertyValueFactories { get; set; } = new();
}
