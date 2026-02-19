using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Building;

/// <summary>
/// Threshold below which sequential processing is used instead of parallel.
/// For small test sets, the overhead of task scheduling exceeds parallelization benefits.
/// </summary>
internal static class ParallelThresholds
{
    /// <summary>
    /// Minimum number of items before parallel processing is used.
    /// Below this threshold, sequential processing avoids task scheduling overhead.
    /// </summary>
    public const int MinItemsForParallel = 8;
}

internal sealed class TestBuilderPipeline
{
    private readonly ITestDataCollector _dataCollector;
    private readonly ITestBuilder _testBuilder;
    private readonly IContextProvider _contextProvider;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public TestBuilderPipeline(
        ITestDataCollector dataCollector,
        ITestBuilder testBuilder,
        IContextProvider contextBuilder,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
        _contextProvider = contextBuilder;
        _eventReceiverOrchestrator = eventReceiverOrchestrator ?? throw new ArgumentNullException(nameof(eventReceiverOrchestrator));
    }

    private TestBuilderContext CreateTestBuilderContext(TestMetadata metadata)
    {
        var testBuilderContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata,
            Events = new TestContextEvents(),
            StateBag = new ConcurrentDictionary<string, object?>()
        };

        // Check for ClassConstructor attribute and set it early if present
        var attributes = metadata.GetOrCreateAttributes();

        // Look for any attribute that inherits from ClassConstructorAttribute
        // This handles both ClassConstructorAttribute and ClassConstructorAttribute<T>
        var classConstructorAttribute = attributes
            .Where(a => a is ClassConstructorAttribute)
            .Cast<ClassConstructorAttribute>()
            .FirstOrDefault();

        if (classConstructorAttribute != null)
        {
            testBuilderContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
        }

        return testBuilderContext;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsAsync(string testSessionId)
    {
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId).ConfigureAwait(false);

        // For this method (non-streaming), we're not in execution mode so no filter optimization
        var buildingContext = new TestBuildingContext(IsForExecution: false, Filter: null);
        return await BuildTestsFromMetadataAsync(collectedMetadata, buildingContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Collects all test metadata without building tests.
    /// This is a lightweight operation used for dependency analysis.
    /// </summary>
    public async Task<IEnumerable<TestMetadata>> CollectTestMetadataAsync(string testSessionId)
    {
        return await _dataCollector.CollectTestsAsync(testSessionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Collects test metadata without building tests, with optional filter-aware pre-filtering.
    /// When a filter with extractable hints is provided, only test sources that could match are enumerated.
    /// </summary>
    public async Task<IEnumerable<TestMetadata>> CollectTestMetadataAsync(string testSessionId, ITestExecutionFilter? filter)
    {
        return await _dataCollector.CollectTestsAsync(testSessionId, filter).ConfigureAwait(false);
    }

    /// <summary>
    /// Streaming version that yields tests as they're built without buffering
    /// </summary>
    /// <param name="testSessionId">The test session identifier</param>
    /// <param name="buildingContext">Context for test building</param>
    /// <param name="metadataFilter">Optional predicate to filter which metadata should be built (null means build all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsStreamingAsync(
        string testSessionId,
        TestBuildingContext buildingContext,
        Func<TestMetadata, bool>? metadataFilter = null,
        CancellationToken cancellationToken = default)
    {
        // Get metadata streaming if supported
        // Fall back to non-streaming collection
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId).ConfigureAwait(false);

        // Apply metadata filter if provided (for dependency-aware filtering optimization)
        if (metadataFilter != null)
        {
            collectedMetadata = collectedMetadata.Where(metadataFilter);
        }

        return await collectedMetadata
            .SelectManyAsync(metadata => BuildTestsFromSingleMetadataAsync(metadata, buildingContext, cancellationToken), cancellationToken: cancellationToken)
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

    /// <summary>
    /// Builds tests from pre-collected metadata.
    /// Use this when metadata has already been collected with filter-aware optimization.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(
        IEnumerable<TestMetadata> testMetadata,
        TestBuildingContext buildingContext,
        CancellationToken cancellationToken = default)
    {
        // Materialize to check count - for small sets, sequential processing is faster
        var metadataList = testMetadata as IList<TestMetadata> ?? testMetadata.ToList();

        IEnumerable<IEnumerable<AbstractExecutableTest>> testGroups;

        if (metadataList.Count < ParallelThresholds.MinItemsForParallel)
        {
            // Sequential processing for small sets - avoids task scheduling overhead
            var results = new List<IEnumerable<AbstractExecutableTest>>(metadataList.Count);
            foreach (var metadata in metadataList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (metadata is IDynamicTestMetadata)
                    {
                        results.Add(await GenerateDynamicTests(metadata).ConfigureAwait(false));
                    }
                    else
                    {
                        results.Add(await _testBuilder.BuildTestsFromMetadataAsync(metadata, buildingContext, cancellationToken).ConfigureAwait(false));
                    }
                }
                catch (Exception ex)
                {
                    var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                    results.Add([failedTest]);
                }
            }
            testGroups = results;
        }
        else
        {
            // Parallel processing for larger sets
            testGroups = await metadataList.SelectAsync(async metadata =>
                {
                    try
                    {
                        // Check if this is a dynamic test metadata that should bypass normal test building
                        if (metadata is IDynamicTestMetadata)
                        {
                            return await GenerateDynamicTests(metadata).ConfigureAwait(false);
                        }

                        return await _testBuilder.BuildTestsFromMetadataAsync(metadata, buildingContext, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                        return (IEnumerable<AbstractExecutableTest>)[failedTest];
                    }
                }, cancellationToken: cancellationToken)
                .ProcessInParallel(Environment.ProcessorCount);
        }

        return testGroups.SelectMany(x => x);
    }

    private async Task<AbstractExecutableTest[]> GenerateDynamicTests(TestMetadata metadata)
    {
        // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
        var repeatCount = metadata.RepeatCount ?? 0;

        // Get dynamic test metadata for DisplayName support
        var dynamicTestMetadata = metadata as IDynamicTestMetadata;

        return await Enumerable.Range(0, repeatCount + 1)
            .SelectAsync(async repeatIndex =>
        {
            // Create a simple TestData for ID generation
            // Use DynamicTestIndex from the metadata to ensure unique test IDs for multiple dynamic tests
            var dynamicTestIndex = dynamicTestMetadata?.DynamicTestIndex ?? 0;
            var testData = new TestBuilder.TestData
            {
                TestClassInstanceFactory = () => Task.FromResult(metadata.InstanceFactory(Type.EmptyTypes, [])),
                ClassDataSourceAttributeIndex = 0,
                ClassDataLoopIndex = 0,
                ClassData = [],
                MethodDataSourceAttributeIndex = 0,
                MethodDataLoopIndex = dynamicTestIndex,
                MethodData = [],
                RepeatIndex = repeatIndex,
                InheritanceDepth = metadata.InheritanceDepth,
                ResolvedClassGenericArguments = Type.EmptyTypes,
                ResolvedMethodGenericArguments = Type.EmptyTypes
            };

            var testId = TestIdentifierService.GenerateTestId(metadata, testData);

            // Use custom DisplayName if specified, otherwise fall back to TestName
            var baseDisplayName = dynamicTestMetadata?.DisplayName ?? metadata.TestName;
            var displayName = repeatCount > 0
                ? $"{baseDisplayName} (Repeat {repeatIndex + 1}/{repeatCount + 1})"
                : baseDisplayName;

            // Get attributes first
            var attributes = metadata.GetOrCreateAttributes();

            // Create TestDetails for dynamic tests
            var testDetails = new TestDetails(attributes)
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
                AttributesByType = attributes.ToAttributeDictionary(),
                Timeout = Core.Defaults.TestTimeout // Default timeout (can be overridden by TimeoutAttribute)
                // Don't set RetryLimit here - let discovery event receivers set it
            };

            var testBuilderContext = CreateTestBuilderContext(metadata);

            var context = _contextProvider.CreateTestContext(
                metadata.TestName,
                metadata.TestClassType,
                testBuilderContext,
                CancellationToken.None);

            // Set the TestDetails on the context
            context.Metadata.TestDetails = testDetails;

            // Set custom display name for dynamic tests if specified
            if (dynamicTestMetadata?.DisplayName != null)
            {
                context.Metadata.DisplayName = dynamicTestMetadata.DisplayName;
            }

            // Invoke discovery event receivers to properly handle all attribute behaviors
            await InvokeDiscoveryEventReceiversAsync(context).ConfigureAwait(false);

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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    private async IAsyncEnumerable<AbstractExecutableTest> BuildTestsFromSingleMetadataAsync(TestMetadata metadata, TestBuildingContext buildingContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

                // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
                var repeatCount = resolvedMetadata.RepeatCount ?? 0;

                // Get attributes for test details
                var attributes = resolvedMetadata.GetOrCreateAttributes();

                // Dynamic tests need to honor attributes like RepeatCount, RetryCount, etc.
                // We'll create multiple test instances based on RepeatCount
                // Use DynamicTestIndex from the metadata to ensure unique test IDs for multiple dynamic tests
                var dynamicTestIndex = ((IDynamicTestMetadata)resolvedMetadata).DynamicTestIndex;
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
                        MethodDataLoopIndex = dynamicTestIndex,
                        MethodData = [],
                        RepeatIndex = repeatIndex,
                        InheritanceDepth = resolvedMetadata.InheritanceDepth,
                        ResolvedClassGenericArguments = Type.EmptyTypes,
                        ResolvedMethodGenericArguments = Type.EmptyTypes
                    };

                    var testId = TestIdentifierService.GenerateTestId(resolvedMetadata, testData);
                    var dynamicMetadata = (IDynamicTestMetadata)resolvedMetadata;
                    var baseDisplayName = dynamicMetadata.DisplayName ?? resolvedMetadata.TestName;
                    var displayName = repeatCount > 0
                        ? $"{baseDisplayName} (Repeat {repeatIndex + 1}/{repeatCount + 1})"
                        : baseDisplayName;

                    // Create TestDetails for dynamic tests
                    var testDetails = new TestDetails(attributes)
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
                        AttributesByType = attributes.ToAttributeDictionary(),
                        Timeout = Core.Defaults.TestTimeout // Default timeout (can be overridden by TimeoutAttribute)
                        // Don't set Timeout and RetryLimit here - let discovery event receivers set them
                    };

                    var context = _contextProvider.CreateTestContext(
                        resolvedMetadata.TestName,
                        resolvedMetadata.TestClassType,
                        CreateTestBuilderContext(resolvedMetadata),
                        CancellationToken.None);

                    // Set the TestDetails on the context
                    context.Metadata.TestDetails = testDetails;

                    // Set custom display name for dynamic tests if specified
                    if (dynamicMetadata.DisplayName != null)
                    {
                        context.Metadata.DisplayName = dynamicMetadata.DisplayName;
                    }

                    // Invoke discovery event receivers to properly handle all attribute behaviors
                    await InvokeDiscoveryEventReceiversAsync(context).ConfigureAwait(false);

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
                var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(resolvedMetadata, buildingContext, cancellationToken).ConfigureAwait(false);
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

        var testDetails = new TestDetails([])
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
            AttributesByType = AttributeDictionaryHelper.Empty,
            Timeout = Core.Defaults.TestTimeout // Default timeout
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            CreateTestBuilderContext(metadata),
            CancellationToken.None);

        context.Metadata.TestDetails = testDetails;


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
                ComputerName = EnvironmentHelper.MachineName,
                TestContext = context
            }
        };
    }

    private AbstractExecutableTest CreateFailedTestForGenericResolutionError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestName} [GENERIC RESOLUTION ERROR]";

        var testDetails = new TestDetails([])
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
            AttributesByType = AttributeDictionaryHelper.Empty,
            Timeout = Core.Defaults.TestTimeout // Default timeout
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            CreateTestBuilderContext(metadata),
            CancellationToken.None);

        context.Metadata.TestDetails = testDetails;

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
                ComputerName = EnvironmentHelper.MachineName,
                TestContext = context
            }
        };
    }

    private async Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.Metadata.TestDetails.TestName,
            context);

        await _eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes event receivers after dependencies have been resolved.
    /// Delegates to TestBuilder.InvokePostResolutionEventsAsync.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    public ValueTask InvokePostResolutionEventsAsync(AbstractExecutableTest test)
        => _testBuilder.InvokePostResolutionEventsAsync(test);

    /// <summary>
    /// Populates dependencies for all tests without invoking event receivers.
    /// This should be called before After(TestDiscovery) hooks run, so that hooks
    /// can access dependency information on any TestContext.
    /// </summary>
    /// <param name="tests">All tests that have had their dependencies resolved</param>
    public void PopulateAllDependencies(IEnumerable<AbstractExecutableTest> tests)
    {
        foreach (var test in tests)
        {
            _testBuilder.PopulateDependenciesOnly(test);
        }
    }

}
