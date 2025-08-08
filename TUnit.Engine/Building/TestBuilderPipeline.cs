using System.Runtime.CompilerServices;
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
    public async IAsyncEnumerable<AbstractExecutableTest> BuildTestsStreamingAsync(
        string testSessionId, 
        HashSet<Type>? filterTypes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dataCollector = _dataCollectorFactory(filterTypes);
        
        // Get metadata streaming if supported
        IAsyncEnumerable<TestMetadata>? streamingMetadata = null;
        if (dataCollector is IStreamingTestDataCollector streamingCollector)
        {
            streamingMetadata = streamingCollector.CollectTestsStreamingAsync(testSessionId, cancellationToken);
        }
        else
        {
            // Fall back to non-streaming collection
            var collectedMetadata = await dataCollector.CollectTestsAsync(testSessionId);
            streamingMetadata = ToAsyncEnumerable(collectedMetadata);
        }

        await foreach (var metadata in streamingMetadata.WithCancellation(cancellationToken))
        {
            // Build and yield tests one at a time
            await foreach (var test in BuildTestsFromSingleMetadataAsync(metadata))
            {
                yield return test;
            }
        }
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
                    // Get attributes first
                    var attributes = metadata.AttributeFactory?.Invoke() ?? [];

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
                            TestClassInstanceFactory = () => Task.FromResult(metadata.InstanceFactory(Type.EmptyTypes, [])),
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
                            TestLineNumber = metadata.LineNumber ?? 0,
                            TestMethodParameterTypes = metadata.ParameterTypes,
                            ReturnType = typeof(Task),
                            MethodMetadata = metadata.MethodMetadata,
                            Attributes = attributes
                            // Don't set Timeout and RetryLimit here - let discovery event receivers set them
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
                            Context = context,
                            TestClassInstanceFactory = testData.TestClassInstanceFactory
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
                testsToYield = new List<AbstractExecutableTest>();
                
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
                        TestLineNumber = resolvedMetadata.LineNumber ?? 0,
                        TestMethodParameterTypes = resolvedMetadata.ParameterTypes,
                        ReturnType = typeof(Task),
                        MethodMetadata = resolvedMetadata.MethodMetadata,
                        Attributes = attributes
                        // Don't set Timeout and RetryLimit here - let discovery event receivers set them
                    };

                    var context = _contextProvider.CreateTestContext(
                        resolvedMetadata.TestName,
                        resolvedMetadata.TestClassType,
                        new TestBuilderContext { TestMetadata = resolvedMetadata.MethodMetadata },
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
    }

}
