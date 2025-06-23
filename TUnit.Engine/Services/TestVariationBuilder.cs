using TUnit.Core.Interfaces;
using TUnit.Core.Models;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified test builder that uses the TestVariation system for dual-mode support.
/// Works with both source generation and reflection modes seamlessly.
/// </summary>
internal class TestVariationBuilder : ITestBuilder
{
    private readonly ITestVariationExpander _testVariationExpander;
    private readonly ITestContextFactory _testContextFactory;
    private readonly IServiceProvider _serviceProvider;

    public TestVariationBuilder(
        ITestVariationExpander testVariationExpander,
        ITestContextFactory testContextFactory,
        IServiceProvider serviceProvider)
    {
        _testVariationExpander = testVariationExpander;
        _testContextFactory = testContextFactory;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public (IReadOnlyList<DiscoveredTest> Tests, IReadOnlyList<DiscoveryFailure> Failures) BuildTests(
        DiscoveryResult discoveryResult)
    {
        var tests = new List<DiscoveredTest>();
        var failures = discoveryResult.DiscoveryFailures.ToList();

        foreach (var definition in discoveryResult.TestDefinitions)
        {
            try
            {
                var builtTests = BuildTests(definition);
                tests.AddRange(builtTests);
            }
            catch (Exception ex)
            {
                failures.Add(new DiscoveryFailure
                {
                    TestId = definition.TestId,
                    Exception = ex,
                    TestFilePath = definition.TestFilePath,
                    TestLineNumber = definition.TestLineNumber,
                    TestMethodName = definition.MethodMetadata.Name,
                });
            }
        }

        return (tests, failures);
    }

    /// <inheritdoc />
    public IEnumerable<DiscoveredTest> BuildTests(DynamicTest dynamicTest)
    {
        return BuildTestsFromDescriptor(dynamicTest);
    }

    /// <inheritdoc />
    public IEnumerable<DiscoveredTest> BuildTests(TestDefinition definition)
    {
        return BuildTestsFromDescriptor(definition);
    }

    /// <inheritdoc />
    public DiscoveredTest BuildTest(TestVariation variation)
    {
        // Create test details from the variation
        var testDetails = CreateTestDetails(variation);

        // Create test context from the variation
        var testContext = CreateTestContext(variation, testDetails);

        // Create the discovered test
        return CreateDiscoveredTest(variation, testDetails, testContext);
    }

    private IEnumerable<DiscoveredTest> BuildTestsFromDescriptor(ITestDescriptor testDescriptor)
    {
        var tests = new List<DiscoveredTest>();

        // Expand the test descriptor into variations asynchronously
        var variationsTask = ExpandTestVariationsAsync(testDescriptor);
        var variations = variationsTask.GetAwaiter().GetResult(); // TODO: Make interface async

        foreach (var variation in variations)
        {
            var test = BuildTest(variation);
            tests.Add(test);
        }

        return tests;
    }

    private async Task<IEnumerable<TestVariation>> ExpandTestVariationsAsync(ITestDescriptor testDescriptor)
    {
        var variations = new List<TestVariation>();

        await foreach (var variation in _testVariationExpander.ExpandTestVariationsAsync(testDescriptor))
        {
            variations.Add(variation);
        }

        return variations;
    }

    private TestDetails CreateTestDetails(TestVariation variation)
    {
        // Create a resettable lazy for the class instance
        var lazyInstance = new ResettableLazy<object>(
            () => CreateInstanceFromVariation(variation),
            variation.TestId,
            variation.TestName);

        // Create test details
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

        // Set retry limit from attributes
        var retryAttribute = variation.MethodMetadata.GetAttribute<RetryAttribute>();
        if (retryAttribute != null)
        {
            testDetails.SetRetryLimit(retryAttribute.Times);
        }

        return testDetails;
    }

    private TestContext CreateTestContext(TestVariation variation, TestDetails testDetails)
    {
        return _testContextFactory.CreateContext(
            testDetails,
            null, // Instance will be created lazily
            variation.ClassArguments,
            variation.MethodArguments);
    }

    private DiscoveredTest CreateDiscoveredTest(TestVariation variation, TestDetails testDetails, TestContext testContext)
    {
        // Create a resettable lazy that uses the variation's execution mode
        var lazyInstance = new ResettableLazy<object>(
            () => CreateInstanceFromVariation(variation),
            variation.TestId,
            testContext);

        // Create method invoker that uses the variation's execution mode
        Func<object, CancellationToken, ValueTask> methodInvoker = async (instance, cancellationToken) =>
        {
            await InvokeMethodFromVariation(variation, instance);
        };

        // Create the discovered test
        var discoveredTest = new UnifiedDiscoveredTest(lazyInstance, methodInvoker)
        {
            TestContext = testContext
        };

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    private object CreateInstanceFromVariation(TestVariation variation)
    {
        // This will be replaced with proper dual-mode instance creation
        // For now, use the source generated factory if available
        if (variation.SourceGeneratedData?.ClassInstanceFactory != null)
        {
            return variation.SourceGeneratedData.ClassInstanceFactory();
        }

        // Fall back to reflection-based creation
        var type = variation.ClassMetadata.Type;
        var args = variation.ClassArguments ?? Array.Empty<object?>();
        
        try
        {
            return Activator.CreateInstance(type, args) 
                ?? throw new InvalidOperationException($"Failed to create instance of {type.FullName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {type.FullName}: {ex.Message}", ex);
        }
    }

    private async Task InvokeMethodFromVariation(TestVariation variation, object instance)
    {
        // This will be replaced with proper dual-mode method invocation
        // For now, use the source generated invoker if available
        if (variation.SourceGeneratedData?.MethodInvoker != null)
        {
            var args = variation.MethodArguments ?? Array.Empty<object?>();
            await variation.SourceGeneratedData.MethodInvoker(instance, args);
            return;
        }

        // Fall back to reflection-based invocation
        var method = GetMethodInfo(variation.MethodMetadata);
        var args = variation.MethodArguments ?? Array.Empty<object?>();
        
        try
        {
            var result = method.Invoke(instance, args);
            
            // Handle async methods
            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to invoke method {variation.MethodMetadata.MethodName} on {variation.ClassMetadata.Type.FullName}: {ex.Message}", 
                ex);
        }
    }

    private static System.Reflection.MethodInfo GetMethodInfo(MethodMetadata methodMetadata)
    {
        var type = methodMetadata.DeclaringType;
        var method = type.GetMethod(methodMetadata.MethodName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException(
                $"Could not find method {methodMetadata.MethodName} on type {type.FullName}");
        }

        return method;
    }
}