using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

/// Orchestrates unified test building with streaming support and error handling
internal sealed class UnifiedTestBuilderPipeline
{
    private readonly Func<HashSet<Type>?, ITestDataCollector> _dataCollectorFactory;
    private readonly ITestBuilder _testBuilder;
    private readonly IContextProvider _contextProvider;

    public UnifiedTestBuilderPipeline(
        Func<HashSet<Type>?, ITestDataCollector> dataCollectorFactory,
        ITestBuilder testBuilder,
        IContextProvider contextBuilder)
    {
        _dataCollectorFactory = dataCollectorFactory ?? throw new ArgumentNullException(nameof(dataCollectorFactory));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
        _contextProvider = contextBuilder;
    }

    public async Task<IEnumerable<ExecutableTest>> BuildTestsAsync(string testSessionId, HashSet<Type>? filterTypes)
    {
        // Stage 1: Collect test metadata
        var dataCollector = _dataCollectorFactory(filterTypes);
        var collectedMetadata = await dataCollector.CollectTestsAsync(testSessionId);

        var executableTests = new List<ExecutableTest>();

        // Stage 2: Resolve generic types
        var resolvedMetadata = new List<TestMetadata>();
        foreach (var metadata in collectedMetadata)
        {
            try
            {
                resolvedMetadata.AddRange([metadata]);
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

    private ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = $"{metadata.TestClassType.Name}.{metadata.TestName}";

        // Create a minimal test context for failed test
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
            DisplayName = displayName,
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
            DisplayName = displayName,
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

}
