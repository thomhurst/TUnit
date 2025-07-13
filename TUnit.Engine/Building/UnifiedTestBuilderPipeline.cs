using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

/// <summary>
/// Main pipeline that orchestrates the unified test building process using the simplified approach
/// </summary>
public sealed class UnifiedTestBuilderPipeline
{
    private readonly ITestDataCollector _dataCollector;
    private readonly IGenericTypeResolver _genericResolver;
    private readonly ITestBuilder _testBuilder;

    public UnifiedTestBuilderPipeline(
        ITestDataCollector dataCollector,
        IGenericTypeResolver genericResolver,
        ITestBuilder testBuilder)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _genericResolver = genericResolver ?? throw new ArgumentNullException(nameof(genericResolver));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
    }

    /// <summary>
    /// Builds all executable tests through the simplified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> BuildTestsAsync(string testSessionId)
    {
        // Stage 1: Collect test metadata
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId);

        // Stage 2: Resolve generic types
        var resolvedMetadata = await _genericResolver.ResolveGenericsAsync(collectedMetadata);

        // Stage 3: Generate data combinations and build tests using the simplified approach
        var executableTests = new List<ExecutableTest>();

        foreach (var metadata in resolvedMetadata)
        {
            try
            {
                // Use TestBuilder's BuildTestsFromMetadataAsync which handles DataCombinationGenerator delegation
                var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(metadata);
                executableTests.AddRange(testsFromMetadata);
            }
            catch (Exception ex)
            {
                // Create a failed test for data generation failures
                var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                executableTests.Add(failedTest);
            }
        }

        return executableTests;
    }
    
    /// <summary>
    /// Builds executable tests as a stream through the pipeline
    /// </summary>
    public async IAsyncEnumerable<ExecutableTest> BuildTestsStreamAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stream from collectors
        await foreach (var metadata in CollectTestsStreamAsync(testSessionId, cancellationToken))
        {
            // Resolve generic types for this metadata
            var resolvedMetadataList = await _genericResolver.ResolveGenericsAsync(new[] { metadata });
            
            foreach (var resolvedMetadata in resolvedMetadataList)
            {
                // Build executable tests
                IEnumerable<ExecutableTest> testsFromMetadata;
                try
                {
                    testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(resolvedMetadata);
                }
                catch (Exception ex)
                {
                    testsFromMetadata = new[] { CreateFailedTestForDataGenerationError(resolvedMetadata, ex) };
                }
                
                foreach (var test in testsFromMetadata)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return test;
                }
            }
        }
    }
    
    private async IAsyncEnumerable<TestMetadata> CollectTestsStreamAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Check if collector supports streaming
        if (_dataCollector is IStreamingTestDataCollector streamingCollector)
        {
            await foreach (var test in streamingCollector.CollectTestsStreamAsync(testSessionId, cancellationToken))
            {
                yield return test;
            }
        }
        else
        {
            // Fall back to collection-based for non-streaming collectors
            var tests = await _dataCollector.CollectTestsAsync(testSessionId);
            foreach (var test in tests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return test;
            }
        }
    }

    private static ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestName} [DATA GENERATION ERROR]";

        // Create a minimal test context for failed test
        var testDetails = new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null,
            TestMethodArguments = [],
            TestClassArguments = [],
            DisplayName = displayName,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            ClassMetadata = MetadataBuilder.CreateClassMetadata(metadata),
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata),
            Attributes = [],
        };

        var context = new TestContext(
            metadata.TestName,
            displayName,
            CancellationToken.None,
            new TUnit.Core.Services.TestServiceProvider())
        {
            TestDetails = testDetails
        };

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            BeforeTestHooks = [],
            AfterTestHooks = [],
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

}

/// <summary>
/// Factory for creating the appropriate pipeline based on the execution mode
/// </summary>
public static class UnifiedTestBuilderPipelineFactory
{
    /// <summary>
    /// Creates a pipeline configured for the specified execution mode
    /// </summary>
    public static UnifiedTestBuilderPipeline CreatePipeline(
        TestExecutionMode executionMode,
        IServiceProvider? serviceProvider = null,
        Assembly[]? assembliesToScan = null)
    {
        var dataCollector = TestDataCollectorFactory.Create(executionMode, assembliesToScan);
        var genericResolver = new Resolvers.AotGenericTypeResolver();
        var testBuilder = new TestBuilder(serviceProvider);

        return new UnifiedTestBuilderPipeline(
            dataCollector,
            genericResolver,
            testBuilder);
    }

    /// <summary>
    /// Creates a pipeline configured for AOT mode (source generation)
    /// </summary>
    public static UnifiedTestBuilderPipeline CreateAotPipeline(
        ITestInvoker testInvoker,
        IServiceProvider? serviceProvider = null)
    {
        return CreatePipeline(TestExecutionMode.SourceGeneration, serviceProvider);
    }

    /// <summary>
    /// Creates a pipeline configured for reflection mode
    /// </summary>
    public static UnifiedTestBuilderPipeline CreateReflectionPipeline(
        Assembly[] assemblies,
        ITestInvoker testInvoker,
        IServiceProvider? serviceProvider = null)
    {
        return CreatePipeline(TestExecutionMode.Reflection, serviceProvider, assemblies);
    }

    /// <summary>
    /// Creates a pipeline with automatic mode detection
    /// </summary>
    public static async Task<UnifiedTestBuilderPipeline> CreateAutoDetectPipelineAsync(
        string testSessionId,
        IServiceProvider? serviceProvider = null,
        Assembly[]? assembliesToScan = null)
    {
        var dataCollector = await TestDataCollectorFactory.CreateAutoDetectAsync(testSessionId, assembliesToScan);
        var genericResolver = new Resolvers.AotGenericTypeResolver();
        var testBuilder = new TestBuilder(serviceProvider);

        return new UnifiedTestBuilderPipeline(
            dataCollector,
            genericResolver,
            testBuilder);
    }
}
