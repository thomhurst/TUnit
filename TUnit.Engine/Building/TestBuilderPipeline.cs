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

    public TestBuilderPipeline(
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
        var dataCollector = _dataCollectorFactory(filterTypes);
        var collectedMetadata = await dataCollector.CollectTestsAsync(testSessionId);

        var executableTests = new List<ExecutableTest>();

        var resolvedMetadata = new List<TestMetadata>();
        foreach (var metadata in collectedMetadata)
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
                var testsFromMetadata = await _testBuilder.BuildTestsFromMetadataAsync(metadata);
                executableTests.AddRange(testsFromMetadata);
            }
            catch (Exception ex)
            {
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
