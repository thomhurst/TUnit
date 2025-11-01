using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
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
            Events = new TestContextEvents(),
            ObjectBag = new ConcurrentDictionary<string, object?>()
        };

        // Check for ClassConstructor attribute and set it early if present
        var attributes = metadata.AttributeFactory();

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
    /// Streaming version that yields tests as they're built without buffering
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsStreamingAsync(
        string testSessionId,
        TestBuildingContext buildingContext,
        CancellationToken cancellationToken = default)
    {
        // Get metadata streaming if supported
        // Fall back to non-streaming collection
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId).ConfigureAwait(false);

        return await collectedMetadata
            .SelectManyAsync(metadata => BuildTestsFromSingleMetadataAsync(metadata, buildingContext), cancellationToken: cancellationToken)
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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT/trimmed scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT scenarios")]
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(IEnumerable<TestMetadata> testMetadata, TestBuildingContext buildingContext)
    {
        var testGroups = await testMetadata.SelectAsync(async metadata =>
            {
                try
                {
                    // Check if this is a dynamic test metadata that should bypass normal test building
                    if (metadata is IDynamicTestMetadata)
                    {
                        return await GenerateDynamicTests(metadata).ConfigureAwait(false);
                    }

                    return await _testBuilder.BuildTestsFromMetadataAsync(metadata, buildingContext).ConfigureAwait(false);
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
        // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
        var repeatCount = metadata.RepeatCount ?? 0;

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

            // Get attributes first
            var attributes = metadata.AttributeFactory();

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
                AttributesByType = attributes.ToAttributeDictionary(),
                Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
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
    private async IAsyncEnumerable<AbstractExecutableTest> BuildTestsFromSingleMetadataAsync(TestMetadata metadata, TestBuildingContext buildingContext)
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
                var attributes = resolvedMetadata.AttributeFactory?.Invoke() ?? [];

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
                        AttributesByType = attributes.ToAttributeDictionary(),
                        Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
                        // Don't set Timeout and RetryLimit here - let discovery event receivers set them
                    };

                    var context = _contextProvider.CreateTestContext(
                        resolvedMetadata.TestName,
                        resolvedMetadata.TestClassType,
                        CreateTestBuilderContext(resolvedMetadata),
                        CancellationToken.None);

                    // Set the TestDetails on the context
                    context.Metadata.TestDetails = testDetails;

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
                var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(resolvedMetadata, buildingContext).ConfigureAwait(false);
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
            AttributesByType = AttributeDictionaryHelper.Empty,
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout
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
            AttributesByType = AttributeDictionaryHelper.Empty,
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout
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

}
