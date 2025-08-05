using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

internal sealed class TestBuilderPipeline
{
    private readonly Func<HashSet<Type>?, ITestDataCollector> _dataCollectorFactory;
    private readonly ITestBuilder _testBuilder;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public TestBuilderPipeline(
        Func<HashSet<Type>?, ITestDataCollector> dataCollectorFactory,
        ITestBuilder testBuilder,
        IContextProvider contextBuilder,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _dataCollectorFactory = dataCollectorFactory ?? throw new ArgumentNullException(nameof(dataCollectorFactory));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
        _contextProvider = contextBuilder;
        _eventReceiverOrchestrator = eventReceiverOrchestrator ?? throw new ArgumentNullException(nameof(eventReceiverOrchestrator));
    }

    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsAsync(string testSessionId, HashSet<Type>? filterTypes)
    {
        var dataCollector = _dataCollectorFactory(filterTypes);
        var collectedMetadata = await dataCollector.CollectTestsAsync(testSessionId);

        return await BuildTestsFromMetadataAsync(collectedMetadata);
    }

    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(IEnumerable<TestMetadata> testMetadata)
    {
        var executableTests = new List<AbstractExecutableTest>();

        var resolvedMetadata = new List<TestMetadata>();
        foreach (var metadata in testMetadata)
        {
            try
            {
                resolvedMetadata.AddRange([metadata]);
            }
            catch (Exception ex)
            {
                var failedTest = CreateFailedTestForGenericResolutionError(metadata, ex);
                executableTests.Add(failedTest);
                continue;
            }
        }

        foreach (var metadata in resolvedMetadata)
        {
            try
            {
                // Check if this is a dynamic test metadata that should bypass normal test building
                if (metadata is IDynamicTestMetadata)
                {
                    // Dynamic tests need to honor attributes like RepeatCount, RetryCount, etc.
                    // We'll create multiple test instances based on RepeatCount
                    for (var repeatIndex = 0; repeatIndex < metadata.RepeatCount + 1; repeatIndex++)
                    {
                        // Create a simple TestData for ID generation
                        var testData = new TestBuilder.TestData
                        {
                            TestClassInstance = null!,
                            ClassDataSourceAttributeIndex = 0,
                            ClassDataLoopIndex = 0,
                            ClassData = [],
                            MethodDataSourceAttributeIndex = 0,
                            MethodDataLoopIndex = 0,
                            MethodData = [],
                            RepeatIndex = repeatIndex,
                            ResolvedClassGenericArguments = Type.EmptyTypes,
                            ResolvedMethodGenericArguments = Type.EmptyTypes
                        };

                        var testId = TestIdentifierService.GenerateTestId(metadata, testData);
                        var displayName = metadata.RepeatCount > 1
                            ? $"{metadata.TestName} (Repeat {repeatIndex + 1}/{metadata.RepeatCount})"
                            : metadata.TestName;

                        // Create TestDetails for dynamic tests
                        var testDetails = new TestDetails
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
                            Attributes = metadata.AttributeFactory?.Invoke() ?? [],
                            Timeout = metadata.TimeoutMs.HasValue
                                ? TimeSpan.FromMilliseconds(metadata.TimeoutMs.Value)
                                : null,
                            RetryLimit = metadata.RetryCount
                        };

                        var context = _contextProvider.CreateTestContext(
                            metadata.TestName,
                            metadata.TestClassType,
                            new TestBuilderContext { TestMetadata = metadata.MethodMetadata },
                            CancellationToken.None);

                        // Set the TestDetails on the context
                        context.TestDetails = testDetails;

                        // Invoke discovery event receivers to properly handle all attribute behaviors
                        await InvokeDiscoveryEventReceiversAsync(context);

                        var executableTestContext = new ExecutableTestCreationContext
                        {
                            TestId = testId,
                            DisplayName = displayName,
                            Arguments = [],
                            ClassArguments = [],
                            Context = context
                        };

                        var executableTest = metadata.CreateExecutableTestFactory(executableTestContext, metadata);
                        executableTests.Add(executableTest);
                    }
                }
                else
                {
                    // Normal test metadata goes through the standard test builder
                    var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(metadata);
                    executableTests.AddRange(testsFromMetadata);
                }
            }
            catch (Exception ex)
            {
                var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                executableTests.Add(failedTest);
            }
        }

        return executableTests;
    }

    private AbstractExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestClassType.Name}.{metadata.TestName}";

        var testDetails = new TestDetails
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
            Attributes = [],
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            },
            CancellationToken.None);

        context.TestDetails = testDetails;


        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context,
            State = TestState.Failed,
            Result = new TestResult
            {
                State = TestState.Failed,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = Environment.MachineName,
                TestContext = context
            }
        };
    }

    private AbstractExecutableTest CreateFailedTestForGenericResolutionError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestName} [GENERIC RESOLUTION ERROR]";

        var testDetails = new TestDetails
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
            Attributes = [],
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            },
            CancellationToken.None);

        context.TestDetails = testDetails;

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context,
            State = TestState.Failed,
            Result = new TestResult
            {
                State = TestState.Failed,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = Environment.MachineName,
                TestContext = context
            }
        };
    }

    private async Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context);

        await _eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);

        discoveredContext.TransferTo(context);
    }

}
