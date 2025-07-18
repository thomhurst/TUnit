using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Services;
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
    private readonly IContextProvider _contextProvider;

    public UnifiedTestBuilderPipeline(
        ITestDataCollector dataCollector,
        IGenericTypeResolver genericResolver,
        ITestBuilder testBuilder,
        IContextProvider contextBuilder)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _genericResolver = genericResolver ?? throw new ArgumentNullException(nameof(genericResolver));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
        _contextProvider = contextBuilder;
    }

    /// <summary>
    /// Builds all executable tests through the simplified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> BuildTestsAsync(string testSessionId)
    {
        // Stage 1: Collect test metadata
        var collectedMetadata = await _dataCollector.CollectTestsAsync(testSessionId);

        var executableTests = new List<ExecutableTest>();

        // Stage 2: Resolve generic types
        var resolvedMetadata = new List<TestMetadata>();
        foreach (var metadata in collectedMetadata)
        {
            try
            {
                var resolved = await _genericResolver.ResolveGenericsAsync(new[] { metadata });
                resolvedMetadata.AddRange(resolved);
            }
            catch (Exception ex)
            {
                // Create a failed test for generic resolution failures
                var failedTest = CreateFailedTestForGenericResolutionError(metadata, ex);
                executableTests.Add(failedTest);
                continue;
            }
        }

        // Stage 3: Generate data combinations and build tests using the simplified approach
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
            IEnumerable<TestMetadata> resolvedMetadataList;
            ExecutableTest? failedTest = null;
            try
            {
                resolvedMetadataList = await _genericResolver.ResolveGenericsAsync(new[] { metadata });
            }
            catch (Exception ex)
            {
                // Create a failed test for generic resolution failures
                failedTest = CreateFailedTestForGenericResolutionError(metadata, ex);
                resolvedMetadataList = Array.Empty<TestMetadata>();
            }
            
            if (failedTest != null)
            {
                yield return failedTest;
                continue;
            }

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

    private ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
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
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            ClassMetadata = MetadataBuilder.CreateClassMetadata(metadata),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = [],
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            CancellationToken.None,
            new TestServiceProvider());
        
        context.TestDetails = testDetails;


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

    private ExecutableTest CreateFailedTestForGenericResolutionError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestName} [GENERIC RESOLUTION ERROR]";

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
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            ClassMetadata = MetadataBuilder.CreateClassMetadata(metadata),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = [],
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            CancellationToken.None,
            new TestServiceProvider());
        
        context.TestDetails = testDetails;

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
