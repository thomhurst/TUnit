using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

using TUnit.Engine.Helpers;
using TUnit.Engine.Configuration;
namespace TUnit.Engine;

/// <summary>
/// Factory for creating executable tests from metadata, replacing multiple builder classes
/// </summary>
public sealed class TestFactory
{
    private readonly ITestInvoker _testInvoker;
    private readonly IHookInvoker _hookInvoker;
    private readonly IDataSourceResolver _dataSourceResolver;
    private readonly IGenericTypeResolver _genericTypeResolver;

    public TestFactory(
        ITestInvoker testInvoker,
        IHookInvoker hookInvoker,
        IDataSourceResolver dataSourceResolver,
        IGenericTypeResolver genericTypeResolver)
    {
        _testInvoker = testInvoker;
        _hookInvoker = hookInvoker;
        _dataSourceResolver = dataSourceResolver;
        _genericTypeResolver = genericTypeResolver;
    }

    /// <summary>
    /// Creates executable tests from metadata, expanding data-driven tests
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> CreateTests(TestMetadata metadata)
    {
        // Handle generic types first
        if (metadata.GenericTypeInfo != null || metadata.GenericMethodInfo != null)
        {
            #pragma warning disable IL3050 // Calling method with RequiresDynamicCodeAttribute
            #pragma warning disable IL2026 // Calling method with RequiresUnreferencedCodeAttribute
            return await CreateGenericTests(metadata);
            #pragma warning restore IL2026
            #pragma warning restore IL3050
        }

        // Check if we're using runtime generic resolution
        if (_genericTypeResolver.GetType().Name == "GenericTypeResolver")
        {
            #pragma warning disable IL3050 // Calling method with RequiresDynamicCodeAttribute
            #pragma warning disable IL2026 // Calling method with RequiresUnreferencedCodeAttribute
            return await CreateTestsWithReflection(metadata);
            #pragma warning restore IL2026
            #pragma warning restore IL3050
        }

        // AOT-safe path
        return await CreateTestsAotSafe(metadata);
    }

    /// <summary>
    /// AOT-safe test creation (source generation path)
    /// </summary>
    private async Task<IEnumerable<ExecutableTest>> CreateTestsAotSafe(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        // Get all test data combinations
        var testDataSets = await ExpandTestData(metadata);

        // Get property data if any
        var propertyDataSets = await ExpandPropertyData(metadata);

        // Create executable test for each data combination
        foreach (var (arguments, argumentsDisplayText, dataSourceIndices) in testDataSets)
        {
            foreach (var propertyData in propertyDataSets.DefaultIfEmpty(new Dictionary<string, object?>()))
            {
                var executableTest = await CreateExecutableTestAotSafe(metadata, arguments, argumentsDisplayText, propertyData, dataSourceIndices);
                tests.Add(executableTest);
            }
        }

        return tests;
    }

    /// <summary>
    /// Reflection-based test creation
    /// </summary>
    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private async Task<IEnumerable<ExecutableTest>> CreateTestsWithReflection(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        // Get all test data combinations
        var testDataSets = await ExpandTestData(metadata);

        // Get property data if any
        var propertyDataSets = await ExpandPropertyData(metadata);

        // Create executable test for each data combination
        foreach (var (arguments, argumentsDisplayText, dataSourceIndices) in testDataSets)
        {
            foreach (var propertyData in propertyDataSets.DefaultIfEmpty(new Dictionary<string, object?>()))
            {
                var executableTest = await CreateExecutableTest(metadata, arguments, argumentsDisplayText, propertyData, dataSourceIndices);
                tests.Add(executableTest);
            }
        }

        return tests;
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private async Task<ExecutableTest> CreateExecutableTest(
        TestMetadata metadata,
        object?[] arguments,
        string argumentsDisplayText,
        Dictionary<string, object?> propertyValues,
        int[] dataSourceIndices)
    {
        var testId = GenerateTestId(metadata, arguments, propertyValues, dataSourceIndices);
        var displayName = GenerateDisplayName(metadata, argumentsDisplayText, propertyValues);

        // Create instance factory
        var createInstance = CreateInstanceFactory(metadata, propertyValues);

        // Create test invoker
        var invokeTest = CreateTestInvoker(metadata, arguments);

        // Create hooks
        var hooks = CreateHooks(metadata);

        var executableTest = new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = arguments,
            CreateInstance = createInstance,
            InvokeTest = invokeTest,
            PropertyValues = propertyValues,
            Hooks = hooks
        };

        // Create test context for discovery
        executableTest.Context = await CreateTestContext(executableTest);

        return executableTest;
    }

    /// <summary>
    /// AOT-safe executable test creation
    /// </summary>
    private async Task<ExecutableTest> CreateExecutableTestAotSafe(
        TestMetadata metadata,
        object?[] arguments,
        string argumentsDisplayText,
        Dictionary<string, object?> propertyValues,
        int[] dataSourceIndices)
    {
        var testId = GenerateTestId(metadata, arguments, propertyValues, dataSourceIndices);
        var displayName = GenerateDisplayName(metadata, argumentsDisplayText, propertyValues);

        // Use pre-compiled invokers from source generation
        var createInstance = metadata.InstanceFactory != null
            ? async () => {
                var instance = metadata.InstanceFactory();
                await InjectProperties(instance, propertyValues);
                return instance;
              }
            : CreateInstanceFactoryAotSafe(metadata, propertyValues);

        // Use pre-compiled test invoker from source generation
        var invokeTest = metadata.TestInvoker != null
            ? (Func<object, Task>)(instance => metadata.TestInvoker(instance, arguments))
            : CreateTestInvokerAotSafe(metadata, arguments);

        // Create hooks
        var hooks = CreateHooks(metadata);

        var executableTest = new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = arguments,
            CreateInstance = createInstance,
            InvokeTest = invokeTest,
            PropertyValues = propertyValues,
            Hooks = hooks
        };

        // Create test context for discovery
        executableTest.Context = await CreateTestContext(executableTest);

        return executableTest;
    }

    private async Task<List<(object?[] arguments, string displayText, int[] dataSourceIndices)>> ExpandTestData(TestMetadata metadata)
    {
        var results = new List<(object?[], string, int[])>();

        if (metadata.DataSources.Length == 0)
        {
            // No data sources, single test with no arguments
            results.Add((Array.Empty<object?>(), string.Empty, Array.Empty<int>()));
            return results;
        }

        // Resolve all data sources with their indices
        var allDataSets = new List<(IEnumerable<object?[]> data, int sourceIndex)>();
        for (int i = 0; i < metadata.DataSources.Length; i++)
        {
            var data = await _dataSourceResolver.ResolveDataSource(metadata.DataSources[i]);
            // Convert to list and add index to each item
            var indexedData = data.Select((item, itemIndex) => item).ToList();
            allDataSets.Add((indexedData, i));
        }

        // Generate cartesian product for matrix/combinatorial tests
        DiscoveryDiagnostics.RecordDataSourceStart($"{metadata.TestName}", allDataSets.Count);
        
        if (allDataSets.Count == 1)
        {
            // Single data source - simpler case
            var (data, sourceIndex) = allDataSets[0];
            int itemIndex = 0;
            foreach (var args in data)
            {
                var displayText = GenerateArgumentsDisplayText(args);
                results.Add((args, displayText, new[] { sourceIndex, itemIndex }));
                itemIndex++;
            }
        }
        else
        {
            // Multiple data sources - cartesian product with tracking
            var dataLists = allDataSets.Select(x => x.data.ToList()).ToList();
            var combinations = CartesianProductWithIndices(dataLists);

            foreach (var (combination, indices) in combinations)
            {
                var flattened = combination.SelectMany(x => x).ToArray();
                var displayText = GenerateArgumentsDisplayText(flattened);
                // Create array of [sourceIndex, itemIndex] pairs
                var dataSourceIndices = new List<int>();
                for (int i = 0; i < allDataSets.Count; i++)
                {
                    dataSourceIndices.Add(allDataSets[i].sourceIndex);
                    dataSourceIndices.Add(indices[i]);
                }
                results.Add((flattened, displayText, dataSourceIndices.ToArray()));
            }
        }

        DiscoveryDiagnostics.RecordTestExpansion(metadata.TestName, results.Count);
        DiscoveryDiagnostics.RecordDataSourceEnd($"{metadata.TestName}", results.Count);

        return results;
    }

    private async Task<List<Dictionary<string, object?>>> ExpandPropertyData(TestMetadata metadata)
    {
        var results = new List<Dictionary<string, object?>>();

        if (metadata.PropertyDataSources.Length == 0)
        {
            return results;
        }

        // Resolve property data sources
        var propertyDataMap = new Dictionary<string, List<object?>>();
        foreach (var propSource in metadata.PropertyDataSources)
        {
            var data = await _dataSourceResolver.ResolveDataSource(propSource.DataSource);
            var values = data.Select(args => args.Length > 0 ? args[0] : null).ToList();
            propertyDataMap[propSource.PropertyName] = values;
        }

        // Generate all combinations
        var combinations = GeneratePropertyCombinations(propertyDataMap);
        results.AddRange(combinations);

        return results;
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, Dictionary<string, object?> propertyValues)
    {
        if (metadata.InstanceFactory != null)
        {
            // AOT-safe path - source generated
            return async () =>
            {
                var instance = metadata.InstanceFactory();
                await InjectProperties(instance, propertyValues);
                return instance;
            };
        }

        // Reflection fallback
        return CreateReflectionInstanceFactory(metadata, propertyValues);
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<Task<object>> CreateReflectionInstanceFactory(TestMetadata metadata, Dictionary<string, object?> propertyValues)
    {
        return async () =>
        {
            var classType = metadata.TestClassType;

            // Handle generic classes (shouldn't happen if CreateGenericTests worked correctly)
            if (classType.IsGenericTypeDefinition)
            {
                throw new InvalidOperationException(
                    $"Generic test class '{metadata.TestClassType.Name}' reached instance factory without type resolution. " +
                    "This indicates a bug in generic test expansion.");
            }

            #pragma warning disable IL2072 // Test class types are known at compile time through source generation
            var instance = Activator.CreateInstance(classType)
                ?? throw new InvalidOperationException($"Failed to create instance of {classType}");
            #pragma warning restore IL2072
            await InjectProperties(instance, propertyValues);
            return instance;
        };
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<object, Task> CreateTestInvoker(TestMetadata metadata, object?[] arguments)
    {
        if (metadata.TestInvoker != null)
        {
            // AOT-safe path - source generated
            return instance => metadata.TestInvoker(instance, arguments);
        }

        // Reflection fallback
        return CreateReflectionTestInvoker(metadata, arguments);
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<object, Task> CreateReflectionTestInvoker(TestMetadata metadata, object?[] arguments)
    {
        if (metadata.MethodInfo == null)
        {
            throw new InvalidOperationException($"No invoker or MethodInfo available for test {metadata.TestName}");
        }

        var methodInfo = metadata.MethodInfo;

        // Handle generic methods
        if (methodInfo.IsGenericMethodDefinition)
        {
            try
            {
                var genericArguments = _genericTypeResolver.ResolveGenericMethodArguments(methodInfo, arguments);
                methodInfo = methodInfo.MakeGenericMethod(genericArguments);
            }
            catch (GenericTypeResolutionException ex)
            {
                throw new InvalidOperationException($"Failed to resolve generic types for test {metadata.TestName}: {ex.Message}", ex);
            }
        }

        var concreteMethodInfo = methodInfo;
        return instance => _testInvoker.InvokeTestMethod(instance, concreteMethodInfo, arguments);
    }

    /// <summary>
    /// AOT-safe instance factory creation
    /// </summary>
    private Func<Task<object>> CreateInstanceFactoryAotSafe(TestMetadata metadata, Dictionary<string, object?> propertyValues)
    {
        // In AOT mode, we should have pre-compiled invokers
        if (metadata.InstanceFactory == null)
        {
            throw new InvalidOperationException(
                $"Test class '{metadata.TestClassType.Name}' does not have a pre-compiled instance invoker. " +
                "Ensure source generation has run correctly.");
        }

        return async () => {
            var instance = metadata.InstanceFactory();
            await InjectProperties(instance, propertyValues);
            return instance;
        };
    }

    /// <summary>
    /// AOT-safe test invoker creation
    /// </summary>
    private Func<object, Task> CreateTestInvokerAotSafe(TestMetadata metadata, object?[] arguments)
    {
        // In AOT mode, we should have pre-compiled invokers
        if (metadata.TestInvoker == null)
        {
            throw new InvalidOperationException(
                $"Test method '{metadata.TestName}' does not have a pre-compiled test invoker. " +
                "Ensure source generation has run correctly.");
        }

        return instance => metadata.TestInvoker(instance, arguments);
    }

    private TestLifecycleHooks CreateHooks(TestMetadata metadata)
    {
        return new TestLifecycleHooks
        {
            BeforeClass = CreateBeforeClassHookInvokers(metadata.Hooks.BeforeClass),
            AfterClass = CreateInstanceHookInvokers(metadata.Hooks.AfterClass),
            BeforeTest = CreateInstanceHookInvokers(metadata.Hooks.BeforeTest),
            AfterTest = CreateInstanceHookInvokers(metadata.Hooks.AfterTest)
        };
    }

    private Func<HookContext, Task>[] CreateBeforeClassHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<HookContext, Task>(async context =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(null, context);
            }
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(null, h.MethodInfo, context);
            }
        })).ToArray();
    }

    private Func<object, HookContext, Task>[] CreateInstanceHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<object, HookContext, Task>(async (instance, context) =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(instance, context);
            }
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(instance, h.MethodInfo, context);
            }
        })).ToArray();
    }


    private Task InjectProperties(object instance, Dictionary<string, object?> propertyValues)
    {
        foreach (var kvp in propertyValues)
        {
            #pragma warning disable IL2075 // Test instance types are known at compile time
            var property = instance.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
            #pragma warning restore IL2075
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, kvp.Value);
            }
        }
        return Task.CompletedTask;
    }

    private static string GenerateTestId(TestMetadata metadata, object?[] arguments, Dictionary<string, object?> propertyValues, int[] dataSourceIndices)
    {
        var parts = new List<string> { metadata.TestId };

        // Add data source indices to make each test unique
        if (dataSourceIndices.Length > 0)
        {
            var dsIndexPart = string.Join(".", dataSourceIndices);
            parts.Add($"ds{dsIndexPart}");
        }

        if (arguments.Length > 0)
        {
            parts.Add($"[{string.Join(",", arguments.Select(FormatArgument))}]");
        }

        if (propertyValues.Count > 0)
        {
            parts.Add($"<{string.Join(",", propertyValues.Select(kv => $"{kv.Key}={FormatArgument(kv.Value)}"))}>");
        }

        return string.Join("_", parts);
    }

    private static string GenerateDisplayName(TestMetadata metadata, string argumentsDisplayText, Dictionary<string, object?> propertyValues)
    {
        var displayName = metadata.TestName;

        if (!string.IsNullOrEmpty(argumentsDisplayText))
        {
            displayName += $"({argumentsDisplayText})";
        }

        if (propertyValues.Count > 0)
        {
            var propDisplay = string.Join(", ", propertyValues.Select(kv => $"{kv.Key}: {FormatArgument(kv.Value)}"));
            displayName += $" [{propDisplay}]";
        }

        return displayName;
    }

    private static string GenerateArgumentsDisplayText(object?[] arguments)
    {
        if (arguments.Length == 0) return string.Empty;
        return string.Join(", ", arguments.Select(FormatArgument));
    }

    private static string FormatArgument(object? arg)
    {
        return arg switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            _ => arg.ToString() ?? "null"
        };
    }


    private async Task ProcessTestDiscoveryAttributes(ExecutableTest test, TestContext context)
    {
        if (test.Metadata.MethodInfo == null)
        {
            // Can't process attributes without reflection info
            return;
        }

        // Create a DiscoveredTestContext wrapper to allow attributes to add properties
        var discoveredContext = new DiscoveredTestContext(
            context.TestName,
            context.DisplayName,
            context.TestDetails
        );

        // Process class-level attributes
        var classAttributes = test.Metadata.TestClassType.GetCustomAttributes(true);

        foreach (var receiver in classAttributes.OfType<ITestDiscoveryEventReceiver>())
        {
            await receiver.OnTestDiscovered(discoveredContext);
        }

        // Process method-level attributes
        var methodAttributes = test.Metadata.MethodInfo.GetCustomAttributes(true);
        foreach (var attribute in methodAttributes)
        {
            if (attribute is ITestDiscoveryEventReceiver receiver)
            {
                try
                {
                    var task = receiver.OnTestDiscovered(discoveredContext);
                    if (task.IsCompletedSuccessfully)
                    {
                        task.GetAwaiter().GetResult();
                    }
                    else
                    {
                        task.AsTask().GetAwaiter().GetResult();
                    }
                }
                catch
                {
                    // Ignore attribute processing errors
                }
            }
        }
    }

    private async Task<TestContext> CreateTestContext(ExecutableTest test)
    {
        // Create ClassMetadata
        var classMetadata = ClassMetadata.GetOrAdd(test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name, () => new ClassMetadata
        {
            Name = test.Metadata.TestClassType.Name,
            Type = test.Metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
            Namespace = test.Metadata.TestClassType.Namespace,
            Assembly = AssemblyMetadata.GetOrAdd(test.Metadata.TestClassType.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata
            {
                Name = test.Metadata.TestClassType.Assembly.GetName().Name ?? "Unknown",
                Attributes = Array.Empty<AttributeMetadata>()
            }),
            Parameters = Array.Empty<ParameterMetadata>(), // TODO: Get from constructor if needed
            Properties = Array.Empty<PropertyMetadata>(), // TODO: Get from type if needed
            Parent = null, // TODO: Get base type if needed
            Attributes = Array.Empty<AttributeMetadata>() // TODO: Get from type attributes if needed
        });

        // Create MethodMetadata
        MethodMetadata methodMetadata;
        if (test.Metadata.MethodInfo != null)
        {
            var methodInfo = test.Metadata.MethodInfo;
            methodMetadata = new MethodMetadata
            {
                Name = methodInfo.Name,
                Type = test.Metadata.TestClassType,
                TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
                Class = classMetadata,
                #pragma warning disable IL2072 // ParameterType comes from test method parameters which are known at compile time
                Parameters = methodInfo.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
                #pragma warning restore IL2072
                {
                    Name = p.Name ?? "param" + p.Position,
                    TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? p.ParameterType.Name),
                    Attributes = new AttributeMetadata[0],
                    ReflectionInfo = p
                }).ToArray(),
                GenericTypeCount = methodInfo.IsGenericMethodDefinition ? methodInfo.GetGenericArguments().Length : 0,
                ReturnTypeReference = TypeReference.CreateConcrete(methodInfo.ReturnType.AssemblyQualifiedName ?? methodInfo.ReturnType.FullName ?? methodInfo.ReturnType.Name),
                ReturnType = methodInfo.ReturnType,
                Attributes = Array.Empty<AttributeMetadata>() // TODO: Get from method attributes if needed
            };
        }
        else
        {
            // Create minimal MethodMetadata when MethodInfo is not available
            methodMetadata = new MethodMetadata
            {
                Name = test.Metadata.TestMethodName,
                Type = test.Metadata.TestClassType,
                TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
                Class = classMetadata,
                Parameters = Array.Empty<ParameterMetadata>(),
                GenericTypeCount = 0,
                ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
                ReturnType = typeof(Task),
                Attributes = Array.Empty<AttributeMetadata>()
            };
        }

        var testDetails = new TestDetails
        {
            TestId = test.TestId,
            TestName = test.Metadata.TestName,
            ClassType = test.Metadata.TestClassType,
            MethodName = test.Metadata.TestMethodName,
            ClassInstance = null, // Will be set during execution
            TestMethodArguments = test.Arguments,
            TestClassArguments = Array.Empty<object?>(),
            DisplayName = test.DisplayName,
            TestFilePath = test.Metadata.FilePath ?? "Unknown",
            TestLineNumber = test.Metadata.LineNumber ?? 0,
            TestMethodParameterTypes = test.Metadata.ParameterTypes,
            ReturnType = test.Metadata.MethodInfo?.ReturnType ?? typeof(Task),
            ClassMetadata = classMetadata,
            MethodMetadata = methodMetadata
        };

        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }

        var context = new TestContext(test.Metadata.TestName, test.DisplayName)
        {
            TestDetails = testDetails,
            CancellationToken = CancellationToken.None,
            InternalDiscoveredTest = null // Will be set by TestDiscoveryService when needed
        };

        // Process attributes that implement ITestDiscoveryEventReceiver
        await ProcessTestDiscoveryAttributes(test, context);

        return context;
    }

    private static IEnumerable<List<object?[]>> CartesianProduct(List<IEnumerable<object?[]>> sets)
    {
        return CartesianProductWithLimits(sets, 0, new CartesianProductState());
    }

    private static IEnumerable<(List<object?[]> combination, int[] indices)> CartesianProductWithIndices(List<List<object?[]>> sets)
    {
        return CartesianProductWithIndicesAndLimits(sets, 0, new CartesianProductState());
    }

    private static IEnumerable<List<object?[]>> CartesianProductWithLimits(
        List<IEnumerable<object?[]>> sets,
        int depth,
        CartesianProductState state)
    {
        var maxDepth = DiscoveryConfiguration.MaxCartesianDepth;
        var maxTotalCombinations = DiscoveryConfiguration.MaxCartesianCombinations;

        // Record diagnostics
        DiscoveryDiagnostics.RecordCartesianProductDepth(depth, sets.Count);

        if (depth > maxDepth)
        {
            throw new InvalidOperationException(
                $"Cartesian product exceeded maximum recursion depth of {maxDepth}. " +
                "This may indicate an excessive number of data source combinations.");
        }

        if (!sets.Any())
        {
            yield return new List<object?[]>();
            yield break;
        }

        var first = sets.First();
        var rest = sets.Skip(1).ToList();
        var restProduct = CartesianProductWithLimits(rest, depth + 1, state).ToList();

        foreach (var item in first)
        {
            foreach (var restItem in restProduct)
            {
                state.TotalCombinations++;

                if (state.TotalCombinations > maxTotalCombinations)
                {
                    throw new InvalidOperationException(
                        $"Cartesian product exceeded maximum combinations limit of {maxTotalCombinations:N0}. " +
                        "Consider reducing the number of data sources or their sizes.");
                }

                var result = new List<object?[]> { item };
                result.AddRange(restItem);
                yield return result;
            }
        }
    }

    private class CartesianProductState
    {
        public int TotalCombinations { get; set; }
    }

    private static IEnumerable<(List<object?[]> combination, int[] indices)> CartesianProductWithIndicesAndLimits(
        List<List<object?[]>> sets,
        int depth,
        CartesianProductState state)
    {
        var maxDepth = DiscoveryConfiguration.MaxCartesianDepth;
        var maxTotalCombinations = DiscoveryConfiguration.MaxCartesianCombinations;

        // Record diagnostics
        DiscoveryDiagnostics.RecordCartesianProductDepth(depth, sets.Count);

        if (depth > maxDepth)
        {
            throw new InvalidOperationException(
                $"Cartesian product exceeded maximum recursion depth of {maxDepth}. " +
                "This may indicate an excessive number of data source combinations.");
        }

        if (!sets.Any())
        {
            yield return (new List<object?[]>(), Array.Empty<int>());
            yield break;
        }

        var first = sets.First();
        var rest = sets.Skip(1).ToList();
        var restProduct = CartesianProductWithIndicesAndLimits(rest, depth + 1, state).ToList();

        for (int i = 0; i < first.Count; i++)
        {
            var item = first[i];
            foreach (var (restCombination, restIndices) in restProduct)
            {
                state.TotalCombinations++;

                if (state.TotalCombinations > maxTotalCombinations)
                {
                    throw new InvalidOperationException(
                        $"Cartesian product exceeded maximum combinations limit of {maxTotalCombinations:N0}. " +
                        "Consider reducing the number of data sources or their sizes.");
                }

                var result = new List<object?[]> { item };
                result.AddRange(restCombination);
                
                var indices = new int[sets.Count];
                indices[0] = i;
                if (restIndices.Length > 0)
                {
                    Array.Copy(restIndices, 0, indices, 1, restIndices.Length);
                }
                
                yield return (result, indices);
            }
        }
    }

    private static List<Dictionary<string, object?>> GeneratePropertyCombinations(Dictionary<string, List<object?>> propertyDataMap)
    {
        if (!propertyDataMap.Any())
        {
            return new List<Dictionary<string, object?>>();
        }

        var results = new List<Dictionary<string, object?>>();
        var properties = propertyDataMap.Keys.ToList();
        var indices = new int[properties.Count];

        const int maxIterations = 1_000_000;
        int iterationCount = 0;

        while (iterationCount < maxIterations)
        {
            var combination = new Dictionary<string, object?>();
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var values = propertyDataMap[prop];
                combination[prop] = values[indices[i]];
            }
            results.Add(combination);

            int position = properties.Count - 1;
            while (position >= 0)
            {
                indices[position]++;
                if (indices[position] < propertyDataMap[properties[position]].Count)
                {
                    break;
                }
                indices[position] = 0;
                position--;
            }

            if (position < 0) break;
            iterationCount++;
        }

        if (iterationCount >= maxIterations)
        {
            throw new InvalidOperationException($"Property data combination generation exceeded maximum iterations ({maxIterations}). This may indicate an infinite loop or excessively large data set.");
        }

        return results;
    }

    /// <summary>
    /// Creates tests for generic types by resolving type parameters from test data
    /// </summary>
    [RequiresDynamicCode("Generic type resolution requires dynamic code generation")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming")]
    private async Task<IEnumerable<ExecutableTest>> CreateGenericTests(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        // Get all test data combinations
        var testDataSets = await ExpandTestData(metadata);

        // Check if this is a generic method with no test data
        if (testDataSets.Count == 1 && testDataSets[0].arguments.Length == 0)
        {
            // Generic methods require arguments to infer type parameters
            var methodInfo = metadata.MethodInfo;
            if (methodInfo?.IsGenericMethodDefinition == true)
            {
                // Include diagnostic information about data sources
                var dataSources = metadata.DataSources?.Length ?? 0;
                throw new GenericTypeResolutionException(
                    $"Generic test method '{metadata.TestClassType.FullName}.{metadata.TestMethodName}' requires " +
                    $"test data to infer generic type parameters. Found {dataSources} data sources but no arguments. " +
                    $"Add [Arguments] attributes or other data sources.");
            }
        }

        // For each data set, try to resolve generic types
        foreach (var (arguments, argumentsDisplayText, dataSourceIndices) in testDataSets)
        {
            // Create a specialized metadata for this specific generic instantiation
            var specializedMetadata = CreateSpecializedMetadata(metadata, arguments);

            // Create tests with the specialized metadata
            var specializedTests = await CreateTestsWithReflection(specializedMetadata);
            tests.AddRange(specializedTests);
        }

        return tests;
    }

    /// <summary>
    /// Creates specialized metadata with resolved generic types
    /// </summary>
    [RequiresDynamicCode("Generic type resolution requires dynamic code generation")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming")]
    private TestMetadata CreateSpecializedMetadata(TestMetadata metadata, object?[] arguments)
    {
        var testClassType = metadata.TestClassType;
        var methodInfo = metadata.MethodInfo;

        // Resolve generic method types first (they might inform class types)
        if (methodInfo?.IsGenericMethodDefinition == true && metadata.GenericMethodInfo != null)
        {
            try
            {
                var genericArguments = _genericTypeResolver.ResolveGenericMethodArguments(methodInfo, arguments);
                methodInfo = methodInfo.MakeGenericMethod(genericArguments);
            }
            catch (GenericTypeResolutionException ex)
            {
                // Add more context about the test being created
                var testName = metadata.TestId ?? $"{metadata.TestClassType.Name}.{methodInfo.Name}";
                throw new GenericTypeResolutionException(
                    $"Failed to resolve generic types for test '{testName}': {ex.Message}", ex);
            }
        }

        // Resolve generic class types
        if (testClassType.IsGenericTypeDefinition && metadata.GenericTypeInfo != null)
        {
            // Try to infer from method parameters if available
            Type[] classGenericArgs;

            if (methodInfo != null)
            {
                // Extract type arguments from resolved method parameter types
                classGenericArgs = InferClassGenericArgumentsFromMethod(testClassType, methodInfo, arguments);
            }
            else
            {
                // Fall back to constructor-based resolution
                classGenericArgs = _genericTypeResolver.ResolveGenericClassArguments(testClassType, Array.Empty<object?>());
            }

            testClassType = testClassType.MakeGenericType(classGenericArgs);

            // Update method info to be from the constructed type
            if (methodInfo != null)
            {
                var methodName = methodInfo.Name;
                var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                methodInfo = testClassType.GetMethod(methodName, paramTypes);
            }
        }

        // Create new metadata with resolved types
        return new TestMetadata
        {
            TestId = metadata.TestId,
            TestName = metadata.TestName,
            TestClassType = testClassType,
            TestMethodName = metadata.TestMethodName,
            Categories = metadata.Categories,
            IsSkipped = metadata.IsSkipped,
            SkipReason = metadata.SkipReason,
            TimeoutMs = metadata.TimeoutMs,
            RetryCount = metadata.RetryCount,
            CanRunInParallel = metadata.CanRunInParallel,
            DependsOn = metadata.DependsOn,
            DataSources = metadata.DataSources,
            PropertyDataSources = metadata.PropertyDataSources,
            InstanceFactory = null, // Will be created with resolved type
            TestInvoker = null, // Will be created with resolved method
            ParameterCount = metadata.ParameterCount,
            ParameterTypes = methodInfo?.GetParameters().Select(p => p.ParameterType).ToArray() ?? metadata.ParameterTypes,
            Hooks = metadata.Hooks,
            MethodInfo = methodInfo,
            FilePath = metadata.FilePath,
            LineNumber = metadata.LineNumber,
            GenericTypeInfo = null, // No longer generic
            GenericMethodInfo = null // No longer generic
        };
    }

    /// <summary>
    /// Infers generic class type arguments from a resolved method and its arguments
    /// </summary>
    private Type[] InferClassGenericArgumentsFromMethod(Type genericClassType, MethodInfo resolvedMethod, object?[] arguments)
    {
        var classTypeParams = genericClassType.GetGenericArguments();
        var resolvedTypes = new Type[classTypeParams.Length];
        var typeMapping = new Dictionary<string, Type>();

        // Build mapping from method parameter types and arguments
        var parameters = resolvedMethod.GetParameters();
        for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            if (arguments[i] != null)
            {
                var paramType = parameters[i].ParameterType;
                var argType = arguments[i]!.GetType();

                // If parameter type contains generic parameters from the class, map them
                ExtractTypeMapping(paramType, argType, genericClassType, typeMapping);
            }
        }

        // Resolve each class type parameter
        for (int i = 0; i < classTypeParams.Length; i++)
        {
            var param = classTypeParams[i];
            if (typeMapping.TryGetValue(param.Name, out var resolvedType))
            {
                resolvedTypes[i] = resolvedType;
            }
            else
            {
                throw new GenericTypeResolutionException(
                    $"Could not resolve generic type parameter '{param.Name}' for class '{genericClassType.Name}'");
            }
        }

        return resolvedTypes;
    }

    /// <summary>
    /// Extracts type mappings from parameter and argument types
    /// </summary>
    private void ExtractTypeMapping(Type paramType, Type argType, Type genericClassType, Dictionary<string, Type> mapping)
    {
        // Check if paramType references any of the class's generic parameters
        if (paramType.IsGenericParameter && paramType.DeclaringType == genericClassType)
        {
            mapping[paramType.Name] = argType;
        }
        else if (paramType.IsGenericType && argType.IsGenericType)
        {
            var paramGenericArgs = paramType.GetGenericArguments();
            var argGenericArgs = argType.GetGenericArguments();

            for (int i = 0; i < paramGenericArgs.Length && i < argGenericArgs.Length; i++)
            {
                ExtractTypeMapping(paramGenericArgs[i], argGenericArgs[i], genericClassType, mapping);
            }
        }
        else if (paramType.IsArray && argType.IsArray)
        {
            ExtractTypeMapping(paramType.GetElementType()!, argType.GetElementType()!, genericClassType, mapping);
        }
    }
}
