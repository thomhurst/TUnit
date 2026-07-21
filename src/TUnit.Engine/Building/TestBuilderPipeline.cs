using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Extensions;
using TUnit.Engine.Services;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Building;

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
        };

        // Check for ClassConstructor attribute and set it early if present
        var attributes = metadata.GetOrCreateAttributes();

        // Look for any attribute that inherits from ClassConstructorAttribute
        // This handles both ClassConstructorAttribute and ClassConstructorAttribute<T>
        var classConstructorAttribute = attributes.FirstOfType<ClassConstructorAttribute>();

        if (classConstructorAttribute != null)
        {
            testBuilderContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
        }

        return testBuilderContext;
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
    /// Collects metadata for the session and builds all tests, optionally pre-filtering metadata.
    /// </summary>
    /// <param name="testSessionId">The test session identifier</param>
    /// <param name="buildingContext">Context for test building</param>
    /// <param name="metadataFilter">Optional predicate to filter which metadata should be built (null means build all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsAsync(
        string testSessionId,
        TestBuildingContext buildingContext,
        Func<TestMetadata, bool>? metadataFilter = null,
        CancellationToken cancellationToken = default)
    {
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId).ConfigureAwait(false);

        // Apply metadata filter if provided (for dependency-aware filtering optimization)
        if (metadataFilter != null)
        {
            collectedMetadata = collectedMetadata.Where(metadataFilter);
        }

        return await BuildTestsFromMetadataAsync(collectedMetadata, buildingContext, cancellationToken).ConfigureAwait(false);
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
        var metadataList = testMetadata as IReadOnlyList<TestMetadata> ?? testMetadata.ToList();

        var testGroups = await ParallelMap.SelectParallelAsync(
            metadataList,
            metadata => BuildTestsForMetadataAsync(metadata, buildingContext, cancellationToken),
            Environment.ProcessorCount,
            cancellationToken).ConfigureAwait(false);

        var totalCount = 0;
        foreach (var group in testGroups)
        {
            totalCount += group.Count;
        }

        var allTests = new List<AbstractExecutableTest>(totalCount);
        foreach (var group in testGroups)
        {
            allTests.AddRange(group);
        }

        return allTests;
    }

    /// <summary>
    /// Builds all tests for a single metadata item. Exceptions surface as a single failed
    /// placeholder test rather than propagating, so one bad data source cannot abort discovery.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    private async Task<IReadOnlyList<AbstractExecutableTest>> BuildTestsForMetadataAsync(
        TestMetadata metadata,
        TestBuildingContext buildingContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Dynamic test metadata bypasses normal test building
            if (metadata is IDynamicTestMetadata)
            {
                return await GenerateDynamicTests(metadata).ConfigureAwait(false);
            }

            var tests = await _testBuilder.BuildTestsFromMetadataAsync(metadata, buildingContext, cancellationToken).ConfigureAwait(false);

            return tests as IReadOnlyList<AbstractExecutableTest> ?? tests.ToList();
        }
        catch (Exception ex)
        {
            return [CreateFailedTestForDataGenerationError(metadata, ex)];
        }
    }

    private async Task<AbstractExecutableTest[]> GenerateDynamicTests(TestMetadata metadata)
    {
        // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
        var repeatCount = metadata.RepeatCount ?? 0;

        // Get dynamic test metadata for DisplayName support
        var dynamicTestMetadata = metadata as IDynamicTestMetadata;

        // Hoist loop-invariant state so repeat iterations share one attribute dictionary and factory delegate
        var attributes = metadata.GetOrCreateAttributes();
        var attributesByType = attributes.ToAttributeDictionary();
        Func<Task<object>> instanceFactory = () => Task.FromResult(metadata.InstanceFactory(Type.EmptyTypes, []));

        return await ParallelMap.ForParallelAsync(
            repeatCount + 1,
            async repeatIndex =>
        {
            // Create a simple TestData for ID generation
            // Use DynamicTestIndex from the metadata to ensure unique test IDs for multiple dynamic tests
            var dynamicTestIndex = dynamicTestMetadata?.DynamicTestIndex ?? 0;
            var testData = new TestBuilder.TestData
            {
                TestClassInstanceFactory = instanceFactory,
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
                TestStartColumnNumber = metadata.StartColumnNumber,
                TestEndLineNumber = metadata.EndLineNumber,
                TestEndColumnNumber = metadata.EndColumnNumber,
                ReturnType = typeof(Task),
                MethodMetadata = metadata.MethodMetadata,
                AttributesByType = attributesByType
            };

            var testBuilderContext = CreateTestBuilderContext(metadata);

            var context = _contextProvider.CreateTestContext(
                metadata.TestClassType,
                testBuilderContext,
                testDetails,
                CancellationToken.None);

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
        },
            Environment.ProcessorCount).ConfigureAwait(false);
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
            TestStartColumnNumber = metadata.StartColumnNumber,
            TestEndLineNumber = metadata.EndLineNumber,
            TestEndColumnNumber = metadata.EndColumnNumber,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            AttributesByType = AttributeDictionaryHelper.Empty
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestClassType,
            CreateTestBuilderContext(metadata),
            testDetails,
            CancellationToken.None);

        var now = DateTimeOffset.UtcNow;

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
                Start = now,
                End = now,
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
