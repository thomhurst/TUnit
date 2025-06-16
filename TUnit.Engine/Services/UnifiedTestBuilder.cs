using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified test builder that constructs tests from raw test data,
/// used by both source generation and reflection modes.
/// </summary>
internal class UnifiedTestBuilder(
    ContextManager contextManager,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Builds a discovered test from TestMetadata.
    /// </summary>
    public DiscoveredTest BuildTest(TestMetadata metadata)
    {
        // Build test details using existing TestMetadata behavior
        var testDetails = metadata.BuildTestDetails();
        
        // Get class hook context
        var classHookContext = contextManager.GetClassHookContext(metadata.TestClassType);
        
        // Create test context
        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            metadata,
            classHookContext
        );
        
        // Handle discovery exceptions
        if (metadata.DiscoveryException is not null)
        {
            testContext.SetResult(metadata.DiscoveryException);
        }
        
        // Run discovery hooks
        RunTestDiscoveryHooks(testDetails, testContext);
        
        // Build discovered test using existing TestMetadata behavior
        var discoveredTest = metadata.BuildDiscoveredTest(testContext);
        
        testContext.InternalDiscoveredTest = discoveredTest;
        
        return discoveredTest;
    }
    
    /// <summary>
    /// Builds multiple tests from dynamic test data.
    /// </summary>
    public IEnumerable<DiscoveredTest> BuildTests(DynamicTest dynamicTest)
    {
        return dynamicTest.BuildTestMetadatas()
            .Select(BuildTest);
    }
    
    /// <summary>
    /// Builds a discovered test from raw test construction data.
    /// </summary>
    public DiscoveredTest BuildTest(TestConstructionData data)
    {
        // Always build using the unified approach
        // Source generators will provide strongly-typed delegates
        // Reflection mode will provide reflection-based delegates
        return BuildUntypedTest(data);
    }
    
    /// <summary>
    /// Builds a typed discovered test from generic test construction data.
    /// This method is AOT-safe as it uses only generic constraints and no reflection.
    /// </summary>
    public DiscoveredTest<TTestClass> BuildTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestConstructionData<TTestClass> data) where TTestClass : class
    {
        // Create a resettable lazy for the class instance
        var resettableLazy = new ResettableLazy<TTestClass>(
            data.TestClassFactory,
            data.TestId,
            data.TestBuilderContext
        );
        
        // Build test details
        var testDetails = new TestDetails<TTestClass>
        {
            TestId = data.TestId,
            LazyClassInstance = resettableLazy,
            TestClassArguments = data.ClassArgumentsProvider(),
            TestMethodArguments = data.MethodArgumentsProvider(),
            TestClassInjectedPropertyArguments = data.PropertiesProvider(),
            CurrentRepeatAttempt = data.CurrentRepeatAttempt,
            RepeatLimit = data.RepeatCount,
            TestMethod = data.TestMethod,
            TestName = data.TestMethod.Name,
            ReturnType = data.TestMethod.ReturnType,
            TestFilePath = data.TestFilePath,
            TestLineNumber = data.TestLineNumber,
            DynamicAttributes = [],
            DataAttributes = data.TestMethod.Attributes.OfType<Attribute>().ToArray()
        };
        
        // Get class hook context
        var classType = data.TestMethod.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);
        
        // Create test context - for now we need to pass null for TestConstructionData
        // until we fully migrate away from TestMetadata
        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            metadata: null!,
            classHookContext
        );
        
        // Handle discovery exceptions
        if (data.DiscoveryException is not null)
        {
            testContext.SetResult(data.DiscoveryException);
        }
        
        // Run discovery hooks
        RunTestDiscoveryHooks(testDetails, testContext);
        
        // Build discovered test
        var discoveredTest = new DiscoveredTest<TTestClass>(resettableLazy)
        {
            TestContext = testContext,
            TestBody = data.TestMethodInvoker
        };
        
        testContext.InternalDiscoveredTest = discoveredTest;
        
        return discoveredTest;
    }
    
    private DiscoveredTest BuildUntypedTest(TestConstructionData data)
    {
        // Create a resettable lazy for the class instance
        var resettableLazy = new ResettableLazy<object>(
            data.TestClassFactory,
            data.TestId,
            data.TestBuilderContext
        );
        
        // Build test details
        var testDetails = new UntypedTestDetails(resettableLazy)
        {
            TestId = data.TestId,
            TestName = data.TestMethod.Name,
            TestMethod = data.TestMethod,
            TestFilePath = data.TestFilePath,
            TestLineNumber = data.TestLineNumber,
            TestClassArguments = data.ClassArgumentsProvider(),
            TestMethodArguments = data.MethodArgumentsProvider(),
            TestClassInjectedPropertyArguments = data.PropertiesProvider(),
            RepeatLimit = data.RepeatCount,
            CurrentRepeatAttempt = data.CurrentRepeatAttempt,
            ReturnType = data.TestMethod.ReturnType,
            DataAttributes = data.TestMethod.Attributes.OfType<Attribute>().ToArray()
        };
        
        // Get class hook context
        var classType = data.TestMethod.Class.Type;
        var classHookContext = contextManager.GetClassHookContext(classType);
        
        // Create test context - for now we need to pass null for TestConstructionData
        // until we fully migrate away from TestMetadata
        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            metadata: null!,
            classHookContext
        );
        
        // Handle discovery exceptions
        if (data.DiscoveryException is not null)
        {
            testContext.SetResult(data.DiscoveryException);
        }
        
        // Run discovery hooks
        RunTestDiscoveryHooks(testDetails, testContext);
        
        // Build discovered test
        var discoveredTest = new UnifiedDiscoveredTest(resettableLazy, data.TestMethodInvoker)
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
}