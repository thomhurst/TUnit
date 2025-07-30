using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
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
                // Check if this is a dynamic test metadata that should bypass normal test building
                if (metadata is IDynamicTestMetadata)
                {
                    // Dynamic tests create their executable test directly without data source processing
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
                        RepeatIndex = 0,
                        ResolvedClassGenericArguments = Type.EmptyTypes,
                        ResolvedMethodGenericArguments = Type.EmptyTypes
                    };
                    
                    var testId = TestIdentifierService.GenerateTestId(metadata, testData);
                    var displayName = metadata.TestName; // Use simple name for dynamic tests
                    
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
                        Attributes = [],
                    };
                    
                    var context = _contextProvider.CreateTestContext(
                        metadata.TestName,
                        metadata.TestClassType,
                        new TestBuilderContext { TestMetadata = metadata.MethodMetadata },
                        CancellationToken.None);
                    
                    // Set the TestDetails on the context
                    context.TestDetails = testDetails;
                    
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
