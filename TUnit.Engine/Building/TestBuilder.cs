using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Core.Tracking;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

internal sealed class TestBuilder : ITestBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _sessionId;
    private readonly IHookCollectionService _hookCollectionService;
    private readonly IContextProvider _contextProvider;

    public TestBuilder(IServiceProvider serviceProvider, string sessionId, IHookCollectionService hookCollectionService, IContextProvider contextProvider)
    {
        _serviceProvider = serviceProvider;
        _sessionId = sessionId;
        _hookCollectionService = hookCollectionService;
        _contextProvider = contextProvider;
    }


    public async Task<IEnumerable<ExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        try
        {
            // Create a context accessor for data generation
            var contextAccessor = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            });

            var classDataAttributeIndex = 0;
            foreach (var classDataSource in GetDataSources(metadata.ClassDataSources))
            {
                classDataAttributeIndex++;

                var classDataLoopIndex = 0;
                await foreach (var classDataFactory in classDataSource.GetDataRowsAsync(
                                   DataGeneratorMetadataCreator.CreateDataGeneratorMetadata
                                   (
                                       testMetadata: metadata,
                                       testSessionId: _sessionId,
                                       generatorType: DataGeneratorType.ClassParameters,
                                       testClassInstance: null,
                                       classInstanceArguments: null,
                                       contextAccessor
                                   )))
                {
                    classDataLoopIndex++;

                    var methodDataAttributeIndex = 0;
                    foreach (var methodDataSource in GetDataSources(metadata.DataSources))
                    {
                        methodDataAttributeIndex++;

                        var classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);

                        var methodDataLoopIndex = 0;
                        await foreach (var methodDataFactory in methodDataSource.GetDataRowsAsync(
                                           DataGeneratorMetadataCreator.CreateDataGeneratorMetadata
                                           (
                                               testMetadata: metadata,
                                               testSessionId: _sessionId,
                                               generatorType: DataGeneratorType.TestParameters,
                                               testClassInstance: null, // We don't have instance yet
                                               classInstanceArguments: classData,
                                               contextAccessor
                                           )))
                        {
                            methodDataLoopIndex++;

                            for (var i = 0; i < metadata.RepeatCount + 1; i++)
                            {
                                classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);
                                var methodData = DataUnwrapper.Unwrap(await methodDataFactory() ?? []);

                                // Create a temporary test data for generic type resolution
                                var tempTestData = new TestData
                                {
                                    TestClassInstance = null!, // Temporary placeholder
                                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                    ClassDataLoopIndex = classDataLoopIndex,
                                    ClassData = classData,
                                    MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                    MethodDataLoopIndex = methodDataLoopIndex,
                                    MethodData = methodData,
                                    RepeatIndex = i
                                };

                                // Resolve generic types for both class and method
                                Type[] resolvedClassGenericArgs = Type.EmptyTypes;
                                Type[] resolvedMethodGenericArgs = Type.EmptyTypes;
                                
                                try
                                {
                                    var resolution = TestGenericTypeResolver.Resolve(metadata, tempTestData);
                                    resolvedClassGenericArgs = resolution.ResolvedClassGenericArguments;
                                    resolvedMethodGenericArgs = resolution.ResolvedMethodGenericArguments;
                                }
                                catch (Exception ex)
                                {
                                    // If generic resolution fails, create a failed test
                                    var failedTest = await CreateFailedTestForDataGenerationError(metadata, ex);
                                    tests.Add(failedTest);
                                    continue;
                                }

                                // Now create the instance with resolved generic arguments
                                var instance = metadata.InstanceFactory(resolvedClassGenericArgs, classData);
                                if (instance is null)
                                {
                                    throw new InvalidOperationException($"Error creating test class instance for {metadata.TestClassType.FullName}.");
                                }
                                
                                // Create the final test data with the actual instance
                                var testData = new TestData
                                {
                                    TestClassInstance = instance,
                                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                    ClassDataLoopIndex = classDataLoopIndex,
                                    ClassData = classData,
                                    MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                    MethodDataLoopIndex = methodDataLoopIndex,
                                    MethodData = methodData,
                                    RepeatIndex = i,
                                    ResolvedClassGenericArguments = resolvedClassGenericArgs,
                                    ResolvedMethodGenericArguments = resolvedMethodGenericArgs
                                };

                                var test = await BuildTestAsync(metadata, testData, contextAccessor.Current);
                                tests.Add(test);

                                contextAccessor.Current = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata
                                };
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If data combination generation fails, create a failed test
            var failedTest = await CreateFailedTestForDataGenerationError(metadata, ex);
            tests.Add(failedTest);
            return tests;
        }

        return tests;
    }

    private static IDataSourceAttribute[] GetDataSources(IDataSourceAttribute[] dataSources)
    {
        if (dataSources.Length == 0)
        {
            return [NoDataSource.Instance];
        }

        return dataSources;
    }

    public async Task<ExecutableTest> BuildTestAsync(TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        // Generate unique test ID
        var testId = TestIdentifierService.GenerateTestId(metadata, testData);

        // Create test context with the provided arguments
        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext);

        // Set the test class instance that was already created
        context.TestDetails.ClassInstance = testData.TestClassInstance;

        // Track all objects from data sources
        TrackDataSourceObjects(context, testData.ClassData, testData.MethodData);

        // Invoke discovery event receivers
        await InvokeDiscoveryEventReceiversAsync(context);

        // Create the executable test (hooks are now collected lazily at execution time)
        var creationContext = new ExecutableTestCreationContext
        {
            TestId = testId,
            DisplayName = context.GetDisplayName(), // Use the display name from context which may have been updated by discovery events
            Arguments = testData.MethodData,
            ClassArguments = testData.ClassData,
            Context = context,
            ResolvedMethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ResolvedClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        return metadata.CreateExecutableTestFactory(creationContext, metadata);
    }


    private ValueTask<TestContext> CreateTestContextAsync(string testId, TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        var testDetails = new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = testData.TestClassInstance,
            TestMethodArguments = testData.MethodData,
            TestClassArguments = testData.ClassData,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = metadata.MethodMetadata.ReturnType ?? typeof(void),
            MethodMetadata = metadata.MethodMetadata,
            Attributes =  metadata.AttributeFactory.Invoke(),
            MethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        foreach (var category in metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            testBuilderContext,
            CancellationToken.None);

        context.TestDetails = testDetails;

        return new ValueTask<TestContext>(context);
    }


    private async Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context);

        // Try to get EventReceiverOrchestrator from service provider
        var eventReceiverOrchestrator = _serviceProvider?.GetService(typeof(EventReceiverOrchestrator)) as EventReceiverOrchestrator;
        if (eventReceiverOrchestrator != null)
        {
            // Use the orchestrator for consistency with other event receivers
            await eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);
        }
        else
        {
            // Fallback to attribute-only if orchestrator not available
            foreach (var attribute in context.TestDetails.Attributes)
            {
                if (attribute is ITestDiscoveryEventReceiver receiver)
                {
                    try
                    {
                        await receiver.OnTestDiscovered(discoveredContext);
                    }
                    catch (Exception ex)
                    {
                        _ = ex;
                    }
                }
            }
        }

        discoveredContext.TransferTo(context);
    }

    private async Task<ExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        return await CreateFailedTestForDataGenerationError(metadata, exception, null);
    }

    private async Task<ExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, string? customDisplayName)
    {
        return await CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination(), customDisplayName);
    }

    private async Task<ExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, TestDataCombination combination, string? customDisplayName)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata, combination);
        var displayName = customDisplayName ?? $"{metadata.TestClassType.Name}.{metadata.TestName}";

        var testDetails = CreateFailedTestDetails(metadata, testId);
        var context = CreateFailedTestContext(metadata, testDetails);

        await InvokeDiscoveryEventReceiversAsync(context);

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context
        };
    }

    private static TestDetails CreateFailedTestDetails(TestMetadata metadata, string testId)
    {
        return new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null!,
            TestMethodArguments = [],
            TestClassArguments = [],
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = metadata.AttributeFactory.Invoke(),
        };
    }

    private TestContext CreateFailedTestContext(TestMetadata metadata, TestDetails testDetails)
    {
        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            },
            CancellationToken.None);

        context.TestDetails = testDetails;

        return context;
    }

    private static void TrackDataSourceObjects(TestContext context, object?[] classArguments, object?[] methodArguments)
    {
        foreach (var arg in classArguments)
        {
            UnifiedObjectTracker.TrackObject(context.Events, arg);
        }

        foreach (var arg in methodArguments)
        {
            UnifiedObjectTracker.TrackObject(context.Events, arg);
        }
    }

    internal class TestData
    {
        public required object TestClassInstance { get; init; }

        public required int ClassDataSourceAttributeIndex { get; init; }
        public required int ClassDataLoopIndex { get; init; }
        public required object?[] ClassData { get; init; }

        public required int MethodDataSourceAttributeIndex { get; init; }
        public required int MethodDataLoopIndex { get; init; }
        public required object?[] MethodData { get; init; }
        public required int RepeatIndex { get; init; }

        /// <summary>
        /// Resolved generic type arguments for the test class.
        /// Will be Type.EmptyTypes if the class is not generic.
        /// </summary>
        public Type[] ResolvedClassGenericArguments { get; set; } = Type.EmptyTypes;

        /// <summary>
        /// Resolved generic type arguments for the test method.
        /// Will be Type.EmptyTypes if the method is not generic.
        /// </summary>
        public Type[] ResolvedMethodGenericArguments { get; set; } = Type.EmptyTypes;
    }
}
