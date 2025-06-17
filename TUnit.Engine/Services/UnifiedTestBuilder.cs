using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

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
    public DiscoveredTest BuildTest(ITestDefinition definition, int currentRepeatAttempt = 1)
    {
        // Use polymorphic dispatch - the definition knows how to build itself
        return definition.BuildTest(this, currentRepeatAttempt);
    }
    
    /// <summary>
    /// Builds a discovered test from a non-generic test definition.
    /// </summary>
    public DiscoveredTest BuildTest(TestDefinition definition, int currentRepeatAttempt)
    {
        return BuildUntypedTest(definition, currentRepeatAttempt);
    }
    
    /// <summary>
    /// Builds a typed discovered test from a generic test definition.
    /// This method is AOT-safe as it uses only generic constraints and no reflection.
    /// </summary>
    public DiscoveredTest<TTestClass> BuildTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition,
        int currentRepeatAttempt) where TTestClass : class
    {
        // Create a resettable lazy for the class instance
        var resettableLazy = new ResettableLazy<TTestClass>(
            definition.TestClassFactory,
            definition.TestId,
            new TestBuilderContext() // Clean context, no longer part of definition
        );
        
        // Build test details
        var testDetails = new TestDetails<TTestClass>
        {
            TestId = definition.TestId,
            LazyClassInstance = resettableLazy,
            TestClassArguments = definition.ClassArgumentsProvider(),
            TestMethodArguments = definition.MethodArgumentsProvider(),
            TestClassInjectedPropertyArguments = definition.PropertiesProvider(),
            CurrentRepeatAttempt = currentRepeatAttempt, // Now passed in, not from definition
            RepeatLimit = definition.RepeatCount,
            TestMethod = definition.TestMethod,
            TestName = definition.TestMethod.Name,
            ReturnType = definition.TestMethod.ReturnType,
            TestFilePath = definition.TestFilePath,
            TestLineNumber = definition.TestLineNumber,
            DynamicAttributes = [],
            DataAttributes = definition.TestMethod.Attributes.OfType<Attribute>().ToArray()
        };
        
        // Get class hook context
        var classType = definition.TestMethod.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);
        
        // Create test execution context for runtime state
        var executionContext = new TestExecutionContext(definition, currentRepeatAttempt);
        
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
        var discoveredTest = new DiscoveredTest<TTestClass>(resettableLazy)
        {
            TestContext = testContext,
            TestBody = definition.TestMethodInvoker
        };
        
        testContext.InternalDiscoveredTest = discoveredTest;
        
        return discoveredTest;
    }
    
    private DiscoveredTest BuildUntypedTest(TestDefinition definition, int currentRepeatAttempt)
    {
        // Create a resettable lazy for the class instance
        var resettableLazy = new ResettableLazy<object>(
            definition.TestClassFactory,
            definition.TestId,
            new TestBuilderContext() // Clean context
        );
        
        // Build test details
        var testDetails = new UntypedTestDetails(resettableLazy)
        {
            TestId = definition.TestId,
            TestName = definition.TestMethod.Name,
            TestMethod = definition.TestMethod,
            TestFilePath = definition.TestFilePath,
            TestLineNumber = definition.TestLineNumber,
            TestClassArguments = definition.ClassArgumentsProvider(),
            TestMethodArguments = definition.MethodArgumentsProvider(),
            TestClassInjectedPropertyArguments = definition.PropertiesProvider(),
            RepeatLimit = definition.RepeatCount,
            CurrentRepeatAttempt = currentRepeatAttempt, // Now passed in
            ReturnType = definition.TestMethod.ReturnType,
            DataAttributes = definition.TestMethod.Attributes.OfType<Attribute>().ToArray()
        };
        
        // Get class hook context
        var classType = definition.TestMethod.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);
        
        // Create test execution context
        var executionContext = new TestExecutionContext(definition, currentRepeatAttempt);
        
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
        var attributes = testDetails.DataAttributes
            .Concat(testDetails.Attributes)
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
        
        foreach (var definition in discoveryResult.TestDefinitions)
        {
            // For each test definition, create discovered tests for each repeat
            for (int repeatAttempt = 1; repeatAttempt <= definition.RepeatCount; repeatAttempt++)
            {
                try
                {
                    var test = BuildTestFromDefinition(definition, repeatAttempt);
                    tests.Add(test);
                }
                catch (Exception ex)
                {
                    // If building a test fails, we should log it but continue
                    // This would be better handled with proper logging
                    Console.WriteLine($"Failed to build test {definition.TestId}: {ex.Message}");
                }
            }
        }
        
        return (tests, discoveryResult.DiscoveryFailures);
    }
    
    private DiscoveredTest BuildTestFromDefinition(ITestDefinition definition, int repeatAttempt)
    {
        // Use polymorphic dispatch - no reflection needed!
        return definition.BuildTest(this, repeatAttempt);
    }
}