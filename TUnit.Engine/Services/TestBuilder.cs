using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

/// <summary>
/// Simplified test builder that uses injected services for all operations.
/// Consolidates functionality from UnifiedTestBuilder, DynamicTestBuilder, and reflection TestBuilder.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal class TestBuilder : ITestBuilder
{
    private readonly ITestMetadataExpander _metadataExpander;
    private readonly ITestContextFactory _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ContextManager _contextManager;

    public TestBuilder(
        ITestMetadataExpander metadataExpander,
        ITestContextFactory contextFactory,
        IServiceProvider serviceProvider)
    {
        _metadataExpander = metadataExpander;
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _contextManager = serviceProvider.GetService(typeof(ContextManager)) as ContextManager 
            ?? throw new InvalidOperationException("ContextManager not found in service provider");
    }

    /// <summary>
    /// Builds discovered tests from a discovery result.
    /// </summary>
    public (IReadOnlyList<DiscoveredTest> Tests, IReadOnlyList<DiscoveryFailure> Failures) BuildTests(
        DiscoveryResult discoveryResult)
    {
        var tests = new List<DiscoveredTest>();
        var failures = discoveryResult.DiscoveryFailures.ToList();

        foreach (var definition in discoveryResult.TestDefinitions)
        {
            try
            {
                var builtTests = BuildTestsFromDefinition(definition);
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

    /// <summary>
    /// Builds multiple tests from dynamic test data.
    /// </summary>
    public IEnumerable<DiscoveredTest> BuildTests(DynamicTest dynamicTest)
    {
        var discoveryResult = dynamicTest.BuildTests();
        var (tests, _) = BuildTests(discoveryResult);
        return tests;
    }

    /// <summary>
    /// Builds discovered tests from a test definition using polymorphic dispatch.
    /// </summary>
    public IEnumerable<DiscoveredTest> BuildTests(ITestDefinition definition)
    {
        return BuildTestsFromDefinition(definition);
    }

    /// <summary>
    /// Builds discovered tests from a non-generic test definition.
    /// Expands data from data providers to create multiple tests.
    /// </summary>
    public IEnumerable<DiscoveredTest> BuildTests(TestDefinition definition)
    {
        // Note: This synchronous method is required by the ITestBuilder interface
        // In the future, consider making the interface async
        var task = BuildUntypedTestsAsync(definition);
        task.Wait();
        return task.Result;
    }

    /// <summary>
    /// Builds typed discovered tests from a generic test definition.
    /// This method is AOT-safe as it uses only generic constraints and no reflection.
    /// </summary>
    public IEnumerable<DiscoveredTest<TTestClass>> BuildTests<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition) where TTestClass : class
    {
        // Note: This synchronous method is required by the ITestBuilder interface
        // In the future, consider making the interface async
        var task = BuildTypedTestsAsync(definition);
        task.Wait();
        return task.Result;
    }

    private IEnumerable<DiscoveredTest> BuildTestsFromDefinition(ITestDefinition definition)
    {
        if (definition is TestDefinitionBase testDef)
        {
            return testDef.BuildTests(this);
        }

        if (definition is TestDefinition nonGenericDef)
        {
            return BuildTests(nonGenericDef);
        }

        throw new NotSupportedException($"Unknown test definition type: {definition.GetType()}");
    }

    private async Task<IEnumerable<DiscoveredTest>> BuildUntypedTestsAsync(TestDefinition definition)
    {
        var discoveredTests = new List<DiscoveredTest>();
        
        // Get all test data combinations from data providers
        var classDataRows = await definition.ClassDataProvider.GetData();
        var methodDataRows = await definition.MethodDataProvider.GetData();
        
        // If no data rows, create single test with empty arguments
        var classArgsList = classDataRows.Any() ? classDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        var methodArgsList = methodDataRows.Any() ? methodDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        
        // Get property values if available
        var propertyValues = definition.PropertiesProvider();
        
        // Get repeat count
        var repeatCount = definition.MethodMetadata.GetAttribute<RepeatAttribute>()?.Times ?? 0;
        
        var testIndex = 0;
        
        // Generate test for each combination of class and method data
        foreach (var classArgs in classArgsList)
        {
            foreach (var methodArgs in methodArgsList)
            {
                for (var repeatIndex = 0; repeatIndex <= repeatCount; repeatIndex++)
                {
                    var testId = definition.TestId
                        .Replace("{TestIndex}", testIndex.ToString())
                        .Replace("{RepeatIndex}", repeatIndex.ToString())
                        .Replace("{ClassDataIndex}", classArgsList.IndexOf(classArgs).ToString())
                        .Replace("{MethodDataIndex}", methodArgsList.IndexOf(methodArgs).ToString());
                    
                    // Create closure that captures the class arguments
                    Func<object> classFactory = definition.OriginalClassFactory != null 
                        ? () => definition.OriginalClassFactory(classArgs)
                        : definition.TestClassFactory;
                        
                    var resettableLazy = new ResettableLazy<object>(
                        classFactory,
                        testId,
                        new TestBuilderContext()
                    );

                    var testDetails = TestDetails.CreateWithRawAttributes(
                        lazyClassInstance: resettableLazy,
                        testId: testId,
                        testName: BuildTestName(definition.MethodMetadata.Name, methodArgs),
                        testMethod: definition.MethodMetadata,
                        testFilePath: definition.TestFilePath,
                        testLineNumber: definition.TestLineNumber,
                        testClassArguments: classArgs,
                        testMethodArguments: methodArgs,
                        testClassInjectedPropertyArguments: propertyValues,
                        returnType: definition.MethodMetadata.ReturnType ?? typeof(void),
                        dataAttributes: definition.MethodMetadata.Attributes.Select(a => a.Instance).ToArray(),
                        dynamicAttributes: []
                    );

                    // Get class hook context
                    var classType = definition.MethodMetadata.Class.Type;
                    var classHookContext = _contextManager.GetClassHookContext(classType);

                    // Create test execution context
                    var executionContext = new TestExecutionContext(definition);

                    // Create test context
                    var testBuilderContext = new TestBuilderContext();

                    var testContext = new TestContext(
                        _serviceProvider,
                        testDetails,
                        definition,
                        testBuilderContext,
                        classHookContext
                    );

                    // Link the execution context for runtime state tracking
                    testContext.ObjectBag["ExecutionContext"] = executionContext;

                    // Run discovery hooks
                    RunTestDiscoveryHooks(testDetails, testContext);

                    // Create closure that captures the method arguments
                    Func<object, CancellationToken, ValueTask> methodInvoker = definition.OriginalMethodInvoker != null
                        ? async (obj, ct) => 
                        {
                            await definition.OriginalMethodInvoker(obj, methodArgs, ct);
                        }
                        : definition.TestMethodInvoker;
                        
                    // Build discovered test
                    var discoveredTest = new UnifiedDiscoveredTest(resettableLazy, methodInvoker)
                    {
                        TestContext = testContext
                    };

                    testContext.InternalDiscoveredTest = discoveredTest;
                    discoveredTests.Add(discoveredTest);
                    testIndex++;
                }
            }
        }
        
        return discoveredTests;
    }

    private async Task<IEnumerable<DiscoveredTest<TTestClass>>> BuildTypedTestsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition) where TTestClass : class
    {
        var discoveredTests = new List<DiscoveredTest<TTestClass>>();
        
        // Get all test data combinations from data providers
        var classDataRows = await definition.ClassDataProvider.GetData();
        var methodDataRows = await definition.MethodDataProvider.GetData();
        
        // If no data rows, create single test with empty arguments
        var classArgsList = classDataRows.Any() ? classDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        var methodArgsList = methodDataRows.Any() ? methodDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        
        // Get property values if available
        var propertyValues = definition.PropertiesProvider();
        
        // Get repeat count
        var repeatCount = definition.MethodMetadata.GetAttribute<RepeatAttribute>()?.Times ?? 0;
        
        var testIndex = 0;
        
        // Generate test for each combination of class and method data
        foreach (var classArgs in classArgsList)
        {
            foreach (var methodArgs in methodArgsList)
            {
                for (var repeatIndex = 0; repeatIndex <= repeatCount; repeatIndex++)
                {
                    var testId = definition.TestId
                        .Replace("{TestIndex}", testIndex.ToString())
                        .Replace("{RepeatIndex}", repeatIndex.ToString())
                        .Replace("{ClassDataIndex}", classArgsList.IndexOf(classArgs).ToString())
                        .Replace("{MethodDataIndex}", methodArgsList.IndexOf(methodArgs).ToString());
                    
                    // Create closure that captures the class arguments
                    Func<TTestClass> classFactory = definition.OriginalClassFactory != null 
                        ? () => definition.OriginalClassFactory(classArgs)
                        : definition.TestClassFactory;
                        
                    var resettableLazy = new ResettableLazy<TTestClass>(
                        classFactory,
                        testId,
                        new TestBuilderContext()
                    );

                    var testDetails = TestDetails.CreateWithRawAttributes(
                        testId: testId,
                        lazyClassInstance: resettableLazy,
                        testClassArguments: classArgs,
                        testMethodArguments: methodArgs,
                        testClassInjectedPropertyArguments: propertyValues,
                        testMethod: definition.MethodMetadata,
                        testName: BuildTestName(definition.MethodMetadata.Name, methodArgs),
                        returnType: definition.MethodMetadata.ReturnType ?? typeof(void),
                        testFilePath: definition.TestFilePath,
                        testLineNumber: definition.TestLineNumber,
                        dynamicAttributes: [],
                        dataAttributes: definition.MethodMetadata.Attributes.Select(a => a.Instance).ToArray()
                    );

                    var classType = definition.MethodMetadata.Class.Type;
                    var classHookContext = _contextManager.GetClassHookContext(classType);

                    // Create test execution context for runtime state
                    var executionContext = new TestExecutionContext(definition);

                    var testBuilderContext = new TestBuilderContext();
                    var testContext = new TestContext(
                        _serviceProvider,
                        testDetails,
                        definition,
                        testBuilderContext,
                        classHookContext
                    );

                    // Link the execution context for runtime state tracking
                    testContext.ObjectBag["ExecutionContext"] = executionContext;

                    RunTestDiscoveryHooks(testDetails, testContext);

                    // Create closure that captures the method arguments
                    Func<TTestClass, CancellationToken, ValueTask> methodInvoker = definition.OriginalMethodInvoker != null
                        ? async (instance, ct) => 
                        {
                            await definition.OriginalMethodInvoker(instance, methodArgs, ct);
                        }
                        : definition.TestMethodInvoker;

                    var discoveredTest = new DiscoveredTest<TTestClass>(resettableLazy)
                    {
                        TestContext = testContext,
                        TestBody = methodInvoker
                    };

                    testContext.InternalDiscoveredTest = discoveredTest;
                    discoveredTests.Add(discoveredTest);
                    testIndex++;
                }
            }
        }
        
        return discoveredTests;
    }
    
    private static string BuildTestName(string baseName, object?[] methodArgs)
    {
        if (methodArgs.Length == 0)
        {
            return baseName;
        }
        
        var argStrings = methodArgs.Select(FormatArgumentValue).ToArray();
        return $"{baseName}({string.Join(", ", argStrings)})";
    }
    
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

    private static void RunTestDiscoveryHooks(TestDetails testDetails, TestContext testContext)
    {
        var attributes = testDetails.DataAttributes.Select(ta => ta.Instance)
            .Concat(testDetails.Attributes.Select(ta => ta.Instance))
            .Distinct();

        DiscoveredTestContext? discoveredTestContext = null;

        // Reverse to run assembly, then class, then method
        foreach (var attribute in attributes.OfType<ITestDiscoveryEventReceiver>().Reverse())
        {
            discoveredTestContext ??= new DiscoveredTestContext(testContext);
            attribute.OnTestDiscovery(discoveredTestContext);
        }
    }
}