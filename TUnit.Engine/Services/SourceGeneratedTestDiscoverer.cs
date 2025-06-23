using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;
using TUnit.Core.Services;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Test discoverer specifically designed for source-generated tests in AOT scenarios.
/// This discoverer works with pre-generated test metadata and AOT-safe execution.
/// </summary>
internal class SourceGeneratedTestDiscoverer
{
    private readonly ISourceGeneratedTestRegistry _testRegistry;
    private readonly ITestVariationExpander _variationExpander;
    private readonly ITestContextFactory _contextFactory;
    private readonly IModeDetector _modeDetector;
    private readonly StronglyTypedFactoryDispatcher _factoryDispatcher;

    public SourceGeneratedTestDiscoverer(
        ISourceGeneratedTestRegistry testRegistry,
        ITestVariationExpander variationExpander,
        ITestContextFactory contextFactory,
        IModeDetector modeDetector)
    {
        _testRegistry = testRegistry;
        _variationExpander = variationExpander;
        _contextFactory = contextFactory;
        _modeDetector = modeDetector;
        _factoryDispatcher = new StronglyTypedFactoryDispatcher((SourceGeneratedTestRegistry)testRegistry);
    }

    /// <summary>
    /// Discovers tests from source-generated metadata.
    /// Only works when source generation mode is detected.
    /// </summary>
    /// <returns>Discovery result with source-generated tests</returns>
    public async Task<SourceGeneratedDiscoveryResult> DiscoverTestsAsync()
    {
        var detectedMode = _modeDetector.DetectMode();
        if (detectedMode != TestExecutionMode.SourceGeneration)
        {
            return new SourceGeneratedDiscoveryResult
            {
                Tests = Array.Empty<DiscoveredTest>(),
                Failures = Array.Empty<DiscoveryFailure>(),
                TestVariations = Array.Empty<TestVariation>(),
                IsSourceGenerationMode = false
            };
        }

        var discoveredTests = new List<DiscoveredTest>();
        var discoveryFailures = new List<DiscoveryFailure>();
        var testVariations = new List<TestVariation>();

        // Get all registered test IDs from the source-generated registry
        var registeredTestIds = _testRegistry.GetRegisteredTestIds();

        foreach (var testId in registeredTestIds)
        {
            try
            {
                // Get pre-resolved data for this test
                var resolvedData = _testRegistry.GetResolvedData(testId);
                if (resolvedData == null)
                {
                    // No pre-resolved data available, skip this test
                    continue;
                }

                // Create test variations from resolved data
                var variations = await CreateTestVariationsFromResolvedData(testId, resolvedData);
                testVariations.AddRange(variations);

                // Convert variations to discovered tests
                foreach (var variation in variations)
                {
                    var discoveredTest = await CreateDiscoveredTestFromVariation(variation);
                    discoveredTests.Add(discoveredTest);
                }
            }
            catch (Exception ex)
            {
                discoveryFailures.Add(new DiscoveryFailure
                {
                    TestId = testId,
                    Exception = ex,
                    TestMethodName = "Unknown"
                });
            }
        }

        return new SourceGeneratedDiscoveryResult
        {
            Tests = discoveredTests.AsReadOnly(),
            Failures = discoveryFailures.AsReadOnly(),
            TestVariations = testVariations.AsReadOnly(),
            IsSourceGenerationMode = true
        };
    }

    /// <summary>
    /// Creates test variations from pre-resolved compile-time data.
    /// </summary>
    private async Task<IEnumerable<TestVariation>> CreateTestVariationsFromResolvedData(
        string baseTestId, 
        CompileTimeResolvedData resolvedData)
    {
        var variations = new List<TestVariation>();

        // Get class and method data combinations
        var classDataList = resolvedData.ClassData.Count > 0 ? resolvedData.ClassData : new List<object?[]> { Array.Empty<object?>() };
        var methodDataList = resolvedData.MethodData.Count > 0 ? resolvedData.MethodData : new List<object?[]> { Array.Empty<object?>() };

        var testIndex = 0;

        // Generate all combinations of class and method data
        foreach (var classData in classDataList)
        {
            var classDataIndex = classDataList.ToList().IndexOf(classData);
            
            foreach (var methodData in methodDataList)
            {
                var methodDataIndex = methodDataList.ToList().IndexOf(methodData);

                // Create unique test ID for this variation
                var variationTestId = $"{baseTestId}_variation_{testIndex++}";

                // Create variation with source-generated data
                var variation = new TestVariation
                {
                    TestId = variationTestId,
                    TestName = GenerateTestName(baseTestId, classData, methodData),
                    ExecutionMode = TestExecutionMode.SourceGeneration,
                    ClassArguments = classData,
                    MethodArguments = methodData,
                    PropertyValues = resolvedData.PropertyData,
                    ClassDataIndex = classDataIndex,
                    MethodDataIndex = methodDataIndex,
                    SourceGeneratedData = new SourceGeneratedTestData
                    {
                        ClassInstanceFactory = _testRegistry.GetClassFactory(baseTestId),
                        MethodInvoker = _testRegistry.GetMethodInvoker(baseTestId),
                        PropertySetters = _testRegistry.GetPropertySetters(baseTestId),
                        CompiledDataSources = new List<object?[]> { classData, methodData }.AsReadOnly()
                    },
                    // These will need to be populated from actual metadata when available
                    MethodMetadata = CreatePlaceholderMethodMetadata(baseTestId),
                    ClassMetadata = CreatePlaceholderClassMetadata(baseTestId)
                };

                variations.Add(variation);
            }
        }

        return variations;
    }

    /// <summary>
    /// Creates a discovered test from a test variation.
    /// </summary>
    private async Task<DiscoveredTest> CreateDiscoveredTestFromVariation(TestVariation variation)
    {
        // Create test details from variation
        var testDetails = CreateTestDetailsFromVariation(variation);

        // Create test context
        var testContext = _contextFactory.CreateContext(
            testDetails,
            null, // Instance will be created lazily
            variation.ClassArguments,
            variation.MethodArguments);

        // Create resettable lazy for instance creation using strongly typed dispatcher
        var lazyInstance = new ResettableLazy<object>(
            () => _factoryDispatcher.CreateTestInstance(variation),
            variation.TestId,
            testContext);

        // Create strongly typed method invoker
        Func<object, CancellationToken, ValueTask> methodInvoker = async (instance, cancellationToken) =>
        {
            try
            {
                // Set properties first (if any)
                _factoryDispatcher.SetInstanceProperties(variation, instance);
                
                // Invoke method using strongly typed dispatcher
                await _factoryDispatcher.InvokeTestMethod(variation, instance);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to invoke test method for {variation.TestId} in source generation mode: {ex.Message}", ex);
            }
        };

        // Create discovered test
        var discoveredTest = new UnifiedDiscoveredTest(lazyInstance, methodInvoker)
        {
            TestContext = testContext
        };

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    /// <summary>
    /// Generates a test name from base ID and data.
    /// </summary>
    private static string GenerateTestName(string baseTestId, object?[] classData, object?[] methodData)
    {
        var baseName = baseTestId.Split('_').FirstOrDefault() ?? baseTestId;
        
        if (classData.Length == 0 && methodData.Length == 0)
        {
            return baseName;
        }

        var allArgs = classData.Concat(methodData).ToArray();
        var argStrings = allArgs.Select(FormatArgumentValue).ToArray();
        return $"{baseName}({string.Join(", ", argStrings)})";
    }

    /// <summary>
    /// Formats an argument value for display in test names.
    /// </summary>
    private static string FormatArgumentValue(object? arg)
    {
        return arg switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLowerInvariant(),
            _ => arg.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Creates test details from a test variation.
    /// </summary>
    private TestDetails CreateTestDetailsFromVariation(TestVariation variation)
    {
        var lazyInstance = new ResettableLazy<object>(
            () => CreateInstanceFromVariation(variation),
            variation.TestId,
            variation.TestName);

        var testDetails = new TestDetails<object>
        {
            TestId = variation.TestId,
            TestName = variation.TestName,
            LazyClassInstance = lazyInstance,
            TestClassArguments = variation.ClassArguments ?? Array.Empty<object?>(),
            TestMethodArguments = variation.MethodArguments ?? Array.Empty<object?>(),
            TestClassInjectedPropertyArguments = variation.PropertyValues ?? new Dictionary<string, object?>(),
            MethodMetadata = variation.MethodMetadata,
            ReturnType = variation.MethodMetadata.ReturnType ?? typeof(void),
            TestFilePath = variation.TestFilePath,
            TestLineNumber = variation.TestLineNumber,
            DataAttributes = Array.Empty<AttributeMetadata>(),
            DynamicAttributes = Array.Empty<AttributeMetadata>()
        };

        // Set timeout if specified
        if (variation.Timeout.HasValue)
        {
            testDetails.SetTimeout(variation.Timeout.Value);
        }

        // Add categories
        foreach (var category in variation.Categories)
        {
            testDetails.Categories.Add(category);
        }

        return testDetails;
    }

    /// <summary>
    /// Creates an instance from a test variation using strongly typed factories.
    /// This method is now deprecated in favor of the StronglyTypedFactoryDispatcher.
    /// </summary>
    [Obsolete("Use StronglyTypedFactoryDispatcher.CreateTestInstance instead")]
    private object CreateInstanceFromVariation(TestVariation variation)
    {
        // Delegate to the strongly typed factory dispatcher
        return _factoryDispatcher.CreateTestInstance(variation);
    }

    /// <summary>
    /// Sets properties on a test instance using source-generated setters.
    /// This method is now deprecated in favor of the StronglyTypedFactoryDispatcher.
    /// </summary>
    [Obsolete("Use StronglyTypedFactoryDispatcher.SetInstanceProperties instead")]
    private void SetPropertiesOnInstance(object instance, TestVariation variation)
    {
        // Delegate to the strongly typed factory dispatcher
        _factoryDispatcher.SetInstanceProperties(variation, instance);
    }

    /// <summary>
    /// Creates placeholder method metadata when actual metadata is not available.
    /// This is a temporary implementation - real metadata should come from source generation.
    /// </summary>
    private static MethodMetadata CreatePlaceholderMethodMetadata(string testId)
    {
        // This is a simplified placeholder - real implementation would get metadata from source generation
        var assemblyMetadata = new AssemblyMetadata
        {
            Assembly = typeof(SourceGeneratedTestDiscoverer).Assembly,
            AssemblyName = typeof(SourceGeneratedTestDiscoverer).Assembly.GetName(),
            Attributes = Array.Empty<AttributeMetadata>()
        };

        var classMetadata = new ClassMetadata
        {
            Type = typeof(object), // Placeholder
            Name = "PlaceholderClass",
            Namespace = "Placeholder",
            Assembly = assemblyMetadata,
            Attributes = Array.Empty<AttributeMetadata>(),
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>(),
            Parent = null,
            TypeReference = new TypeReference { Type = typeof(object) }
        };

        return new MethodMetadata
        {
            Type = typeof(object), // Placeholder
            Name = testId.Split('_').FirstOrDefault() ?? "PlaceholderMethod",
            Class = classMetadata,
            Attributes = Array.Empty<AttributeMetadata>(),
            Parameters = Array.Empty<ParameterMetadata>(),
            GenericTypeCount = 0,
            ReturnType = typeof(void),
            ReturnTypeReference = new TypeReference { Type = typeof(void) },
            TypeReference = new TypeReference { Type = typeof(object) }
        };
    }

    /// <summary>
    /// Creates placeholder class metadata when actual metadata is not available.
    /// This is a temporary implementation - real metadata should come from source generation.
    /// </summary>
    private static ClassMetadata CreatePlaceholderClassMetadata(string testId)
    {
        var assemblyMetadata = new AssemblyMetadata
        {
            Assembly = typeof(SourceGeneratedTestDiscoverer).Assembly,
            AssemblyName = typeof(SourceGeneratedTestDiscoverer).Assembly.GetName(),
            Attributes = Array.Empty<AttributeMetadata>()
        };

        return new ClassMetadata
        {
            Type = typeof(object), // Placeholder
            Name = "PlaceholderClass",
            Namespace = "Placeholder",
            Assembly = assemblyMetadata,
            Attributes = Array.Empty<AttributeMetadata>(),
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>(),
            Parent = null,
            TypeReference = new TypeReference { Type = typeof(object) }
        };
    }
}

/// <summary>
/// Result of source-generated test discovery.
/// </summary>
internal sealed class SourceGeneratedDiscoveryResult
{
    /// <summary>
    /// Discovered tests ready for execution.
    /// </summary>
    public IReadOnlyList<DiscoveredTest> Tests { get; init; } = Array.Empty<DiscoveredTest>();

    /// <summary>
    /// Any failures that occurred during discovery.
    /// </summary>
    public IReadOnlyList<DiscoveryFailure> Failures { get; init; } = Array.Empty<DiscoveryFailure>();

    /// <summary>
    /// All test variations that were created.
    /// </summary>
    public IReadOnlyList<TestVariation> TestVariations { get; init; } = Array.Empty<TestVariation>();

    /// <summary>
    /// Whether source generation mode was active during discovery.
    /// </summary>
    public bool IsSourceGenerationMode { get; init; }

    /// <summary>
    /// Total number of tests discovered.
    /// </summary>
    public int TestCount => Tests.Count;

    /// <summary>
    /// Total number of variations created.
    /// </summary>
    public int VariationCount => TestVariations.Count;
}