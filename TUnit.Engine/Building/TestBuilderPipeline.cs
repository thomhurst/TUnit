using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;
using TUnit.Engine.Utilities;

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

    /// <summary>
    /// Streaming version that yields tests as they're built without buffering
    /// </summary>
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsStreamingAsync(
        string testSessionId,
        HashSet<Type>? filterTypes,
        CancellationToken cancellationToken = default)
    {
        var dataCollector = _dataCollectorFactory(filterTypes);

        // Get metadata streaming if supported
        // Fall back to non-streaming collection
        var collectedMetadata = await dataCollector.CollectTestsAsync(testSessionId);

        return await collectedMetadata
            .SelectManyAsync(BuildTestsFromSingleMetadataAsync, cancellationToken: cancellationToken)
            .ProcessInParallel(cancellationToken: cancellationToken);
    }

    private async IAsyncEnumerable<TestMetadata> ToAsyncEnumerable(IEnumerable<TestMetadata> metadata)
    {
        await Task.Yield(); // Yield control once at the start to maintain async context
        foreach (var item in metadata)
        {
            yield return item;
        }
    }

    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(IEnumerable<TestMetadata> testMetadata)
    {
        var testGroups = await testMetadata.SelectAsync(async metadata =>
            {
                try
                {
                    // Check if this is a dynamic test metadata that should bypass normal test building
                    if (metadata is IDynamicTestMetadata)
                    {
                        return await GenerateDynamicTests(metadata);
                    }

                    return await _testBuilder.BuildTestsFromMetadataAsync(metadata);
                }
                catch (Exception ex)
                {
                    var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                    return [failedTest];
                }
            })
            .ProcessInParallel(Environment.ProcessorCount);

        return testGroups.SelectMany(x => x);
    }

    private async Task<AbstractExecutableTest[]> GenerateDynamicTests(TestMetadata metadata)
    {
        // Get attributes first
        var attributes = metadata.AttributeFactory() ?? [];

        // Extract repeat count from attributes
        var filteredAttributes = ScopedAttributeFilter.FilterScopedAttributes(attributes);
        var repeatAttr = filteredAttributes.OfType<RepeatAttribute>().FirstOrDefault();
        var repeatCount = repeatAttr?.Times ?? 0;

        return await Enumerable.Range(0, repeatCount + 1)
            .SelectAsync(async repeatIndex =>
        {
            // Create a simple TestData for ID generation
            var testData = new TestBuilder.TestData
            {
                TestClassInstanceFactory = () => Task.FromResult(metadata.InstanceFactory(Type.EmptyTypes, [])),
                ClassDataSourceAttributeIndex = 0,
                ClassDataLoopIndex = 0,
                ClassData = [],
                MethodDataSourceAttributeIndex = 0,
                MethodDataLoopIndex = 0,
                MethodData = [],
                RepeatIndex = repeatIndex,
                InheritanceDepth = metadata.InheritanceDepth,
                ResolvedClassGenericArguments = Type.EmptyTypes,
                ResolvedMethodGenericArguments = Type.EmptyTypes
            };

            var testId = TestIdentifierService.GenerateTestId(metadata, testData);
            var displayName = repeatCount > 0
                ? $"{metadata.TestName} (Repeat {repeatIndex + 1}/{repeatCount + 1})"
                : metadata.TestName;

            // Create TestDetails for dynamic tests
            var testDetails = new TestDetails
            {
                TestId = testId,
                TestName = metadata.TestName,
                ClassType = metadata.TestClassType,
                MethodName = metadata.TestMethodName,
                ClassInstance = PlaceholderInstance.Instance,
                TestMethodArguments = [],
                TestClassArguments = [],
                TestFilePath = metadata.FilePath ?? "Unknown",
                TestLineNumber = metadata.LineNumber,
                ReturnType = typeof(Task),
                MethodMetadata = metadata.MethodMetadata,
                Attributes = attributes,
                Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
                // Don't set RetryLimit here - let discovery event receivers set it
            };

            var context = _contextProvider.CreateTestContext(
                metadata.TestName,
                metadata.TestClassType,
                new TestBuilderContext
                {
                    TestMetadata = metadata.MethodMetadata,
                    Events = new TestContextEvents(),
                    ObjectBag = new Dictionary<string, object?>()
                },
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
                Context = context,
                TestClassInstanceFactory = testData.TestClassInstanceFactory
            };

            return metadata.CreateExecutableTestFactory(executableTestContext, metadata);
        })
            .ProcessInParallel(Environment.ProcessorCount);
    }

    /// <summary>
    /// Build tests from a single metadata item, yielding them as they're created
    /// </summary>
    private async IAsyncEnumerable<AbstractExecutableTest> BuildTestsFromSingleMetadataAsync(TestMetadata metadata)
    {
        TestMetadata resolvedMetadata;
        Exception? resolutionError = null;

        try
        {
            resolvedMetadata = metadata;
        }
        catch (Exception ex)
        {
            resolutionError = ex;
            resolvedMetadata = metadata; // Use original for error reporting
        }

        if (resolutionError != null)
        {
            yield return CreateFailedTestForGenericResolutionError(metadata, resolutionError);
            yield break;
        }

        List<AbstractExecutableTest>? testsToYield = null;
        Exception? buildError = null;

        try
        {
            // Check if this is a dynamic test metadata that should bypass normal test building
            if (resolvedMetadata is IDynamicTestMetadata)
            {
                testsToYield =
                [
                ];

                // Get attributes first
                var attributes = resolvedMetadata.AttributeFactory?.Invoke() ?? [];

                // Extract repeat count from attributes
                var filteredAttributes = ScopedAttributeFilter.FilterScopedAttributes(attributes);
                var repeatAttr = filteredAttributes.OfType<RepeatAttribute>().FirstOrDefault();
                var repeatCount = repeatAttr?.Times ?? 0;

                // Dynamic tests need to honor attributes like RepeatCount, RetryCount, etc.
                // We'll create multiple test instances based on RepeatCount
                for (var repeatIndex = 0; repeatIndex < repeatCount + 1; repeatIndex++)
                {
                    // Create a simple TestData for ID generation
                    var testData = new TestBuilder.TestData
                    {
                        TestClassInstanceFactory = () => Task.FromResult(resolvedMetadata.InstanceFactory(Type.EmptyTypes, [])),
                        ClassDataSourceAttributeIndex = 0,
                        ClassDataLoopIndex = 0,
                        ClassData = [],
                        MethodDataSourceAttributeIndex = 0,
                        MethodDataLoopIndex = 0,
                        MethodData = [],
                        RepeatIndex = repeatIndex,
                        InheritanceDepth = resolvedMetadata.InheritanceDepth,
                        ResolvedClassGenericArguments = Type.EmptyTypes,
                        ResolvedMethodGenericArguments = Type.EmptyTypes
                    };

                    var testId = TestIdentifierService.GenerateTestId(resolvedMetadata, testData);
                    var displayName = repeatCount > 0
                        ? $"{resolvedMetadata.TestName} (Repeat {repeatIndex + 1}/{repeatCount + 1})"
                        : resolvedMetadata.TestName;

                    // Create TestDetails for dynamic tests
                    var testDetails = new TestDetails
                    {
                        TestId = testId,
                        TestName = resolvedMetadata.TestName,
                        ClassType = resolvedMetadata.TestClassType,
                        MethodName = resolvedMetadata.TestMethodName,
                        ClassInstance = PlaceholderInstance.Instance,
                        TestMethodArguments = [],
                        TestClassArguments = [],
                        TestFilePath = resolvedMetadata.FilePath ?? "Unknown",
                        TestLineNumber = resolvedMetadata.LineNumber,
                        ReturnType = typeof(Task),
                        MethodMetadata = resolvedMetadata.MethodMetadata,
                        Attributes = attributes,
                        Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
                        // Don't set Timeout and RetryLimit here - let discovery event receivers set them
                    };

                    var context = _contextProvider.CreateTestContext(
                        resolvedMetadata.TestName,
                        resolvedMetadata.TestClassType,
                        new TestBuilderContext 
                        { 
                            TestMetadata = resolvedMetadata.MethodMetadata,
                            Events = new TestContextEvents(),
                            ObjectBag = new Dictionary<string, object?>()
                        },
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
                        Context = context,
                        TestClassInstanceFactory = testData.TestClassInstanceFactory
                    };

                    var executableTest = resolvedMetadata.CreateExecutableTestFactory(executableTestContext, resolvedMetadata);
                    testsToYield.Add(executableTest);
                }
            }
            else
            {
                // Normal test metadata goes through the standard test builder
                var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(resolvedMetadata);
                testsToYield = new List<AbstractExecutableTest>(testsFromMetadata);
            }
        }
        catch (Exception ex)
        {
            buildError = ex;
        }

        if (buildError != null)
        {
            yield return CreateFailedTestForDataGenerationError(resolvedMetadata, buildError);
        }
        else if (testsToYield != null)
        {
            foreach (var test in testsToYield)
            {
                yield return test;
            }
        }
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
            TestLineNumber = metadata.LineNumber,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = [],
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>()
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
            TestLineNumber = metadata.LineNumber,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = [],
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>()
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
    }

}
