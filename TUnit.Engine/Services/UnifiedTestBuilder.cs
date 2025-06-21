using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified test builder that constructs tests from test definitions,
/// used by both source generation and reflection modes.
/// </summary>
internal class UnifiedTestBuilder(
    ContextManager contextManager,
    IServiceProvider serviceProvider) : ITestBuilder
{
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
    /// Builds a discovered test from a test definition using polymorphic dispatch.
    /// </summary>
    public DiscoveredTest BuildTest(ITestDefinition definition)
    {
        // Use polymorphic dispatch - the definition knows how to build itself
        if (definition is TestDefinitionBase testDef)
        {
            return testDef.BuildTest(this);
        }

        throw new NotSupportedException($"Unknown test definition type: {definition.GetType()}");
    }

    /// <summary>
    /// Builds a discovered test from a non-generic test definition.
    /// </summary>
    public DiscoveredTest BuildTest(TestDefinition definition)
    {
        return BuildUntypedTest(definition);
    }

    /// <summary>
    /// Builds a typed discovered test from a generic test definition.
    /// This method is AOT-safe as it uses only generic constraints and no reflection.
    /// </summary>
    public DiscoveredTest<TTestClass> BuildTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition) where TTestClass : class
    {
        var resettableLazy = new ResettableLazy<TTestClass>(
            definition.TestClassFactory,
            definition.TestId,
            new TestBuilderContext()
        );

        var testDetails = TestDetails.CreateWithRawAttributes(
            testId: definition.TestId,
            lazyClassInstance: resettableLazy,
            testClassArguments: definition.ClassArgumentsProvider(),
            testMethodArguments: definition.MethodArgumentsProvider(),
            testClassInjectedPropertyArguments: definition.PropertiesProvider(),
            testMethod: definition.MethodMetadata,
            testName: definition.MethodMetadata.Name,
            returnType: definition.MethodMetadata.ReturnType ?? typeof(void),
            testFilePath: definition.TestFilePath,
            testLineNumber: definition.TestLineNumber,
            dynamicAttributes: [],
            dataAttributes: definition.MethodMetadata.Attributes.Select(a => a.Instance).ToArray()
        );

        var classType = definition.MethodMetadata.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);

        // Create test execution context for runtime state
        var executionContext = new TestExecutionContext(definition);

        var testBuilderContext = new TestBuilderContext();
        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            definition,
            testBuilderContext,
            classHookContext
        );

        // Link the execution context for runtime state tracking
        testContext.ObjectBag["ExecutionContext"] = executionContext;

        RunTestDiscoveryHooks(testDetails, testContext);

        var discoveredTest = new DiscoveredTest<TTestClass>(resettableLazy)
        {
            TestContext = testContext,
            TestBody = definition.TestMethodInvoker
        };

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    private DiscoveredTest BuildUntypedTest(TestDefinition definition)
    {
        var resettableLazy = new ResettableLazy<object>(
            definition.TestClassFactory,
            definition.TestId,
            new TestBuilderContext()
        );

        var testDetails = TestDetails.CreateWithRawAttributes(
            lazyClassInstance: resettableLazy,
            testId: definition.TestId,
            testName: definition.MethodMetadata.Name,
            testMethod: definition.MethodMetadata,
            testFilePath: definition.TestFilePath,
            testLineNumber: definition.TestLineNumber,
            testClassArguments: definition.ClassArgumentsProvider(),
            testMethodArguments: definition.MethodArgumentsProvider(),
            testClassInjectedPropertyArguments: definition.PropertiesProvider(),
            returnType: definition.MethodMetadata.ReturnType ?? typeof(void),
            dataAttributes: definition.MethodMetadata.Attributes.Select(a => a.Instance).ToArray(),
            dynamicAttributes: []
        );

        // Get class hook context
        var classType = definition.MethodMetadata.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);

        // Create test execution context
        var executionContext = new TestExecutionContext(definition);

        // Create test context
        var testBuilderContext = new TestBuilderContext();

        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            definition,
            testBuilderContext,
            classHookContext
        );

        // Link the execution context for runtime state tracking
        testContext.ObjectBag["ExecutionContext"] = executionContext;

        // Run discovery hooks
        RunTestDiscoveryHooks(testDetails, testContext);

        // Build discovered test
        var discoveredTest = new UnifiedDiscoveredTest(resettableLazy, definition.TestMethodInvoker)
        {
            TestContext = testContext
        };

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
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
            var repeatCount = definition.MethodMetadata.GetAttribute<RepeatAttribute>()?.Times ?? 0;
            for (var repeatAttempt = 1; repeatAttempt <= repeatCount + 1; repeatAttempt++)
            {
                try
                {
                    var test = BuildTestFromDefinition(definition);
                    tests.Add(test);
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
        }

        return (tests, failures);
    }

    private DiscoveredTest BuildTestFromDefinition(ITestDefinition definition)
    {
        if (definition is TestDefinitionBase testDef)
        {
            return testDef.BuildTest(this);
        }

        throw new NotSupportedException($"Unknown test definition type: {definition.GetType()}");
    }
}
