using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building;

/// <summary>
/// Main pipeline that orchestrates the unified test building process
/// </summary>
public sealed class UnifiedTestBuilderPipeline
{
    private readonly ITestDataCollector _dataCollector;
    private readonly IGenericTypeResolver _genericResolver;
    private readonly IDataSourceExpander _dataSourceExpander;
    private readonly ITestBuilder _testBuilder;

    public UnifiedTestBuilderPipeline(
        ITestDataCollector dataCollector,
        IGenericTypeResolver genericResolver,
        IDataSourceExpander dataSourceExpander,
        ITestBuilder testBuilder)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _genericResolver = genericResolver ?? throw new ArgumentNullException(nameof(genericResolver));
        _dataSourceExpander = dataSourceExpander ?? throw new ArgumentNullException(nameof(dataSourceExpander));
        _testBuilder = testBuilder ?? throw new ArgumentNullException(nameof(testBuilder));
    }

    /// <summary>
    /// Builds all executable tests through the pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> BuildTestsAsync()
    {
        // Stage 1: Collect test metadata
        var collectedMetadata = await _dataCollector.CollectTestsAsync();

        // Stage 2: Resolve generic types
        var resolvedMetadata = await _genericResolver.ResolveGenericsAsync(collectedMetadata);

        // Stage 3 & 4: Expand data sources and build tests
        var executableTests = new List<ExecutableTest>();

        foreach (var metadata in resolvedMetadata)
        {
            try
            {
                // Stage 3: Expand data sources for this test
                var expandedDataSets = await _dataSourceExpander.ExpandDataSourcesAsync(metadata);

                // Stage 4: Build executable test for each variation
                foreach (var expandedData in expandedDataSets)
                {
                    var executableTest = await _testBuilder.BuildTestAsync(expandedData);
                    executableTests.Add(executableTest);
                }
            }
            catch (Exception ex)
            {
                // Create a failed test node for data source expansion failures
                var failedTest = CreateFailedTestForDataSourceError(metadata, ex);
                executableTests.Add(failedTest);
            }
        }

        return executableTests;
    }

    private static ExecutableTest CreateFailedTestForDataSourceError(TestMetadata metadata, Exception exception)
    {
        var testId = metadata.TestId ?? $"{metadata.TestClassType.FullName}.{metadata.TestMethodName}_DataSourceError";
        var displayName = $"{metadata.TestName} [DATA SOURCE ERROR]";

        return new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            // Create instance that throws the exception
            CreateInstance = () => throw new InvalidOperationException(
                $"Failed to expand data source for test '{displayName}': {exception.Message}", exception),
            // Test method that throws the exception
            InvokeTest = instance => throw new InvalidOperationException(
                $"Failed to expand data source for test '{displayName}': {exception.Message}", exception),
            Hooks = new TestLifecycleHooks
            {
                BeforeClass = [
                ],
                AfterClass = [
                ],
                BeforeTest = [
                ],
                AfterTest = [
                ]
            },
            State = TestState.Failed,
            Result = new TestResult
            {
                Status = Status.Failed,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = Environment.MachineName,
                TestContext = null
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
    /// Creates a pipeline configured for AOT mode (source generation)
    /// </summary>
    public static UnifiedTestBuilderPipeline CreateAotPipeline(
        ITestMetadataSource metadataSource,
        ITestInvoker testInvoker,
        IHookInvoker hookInvoker,
        IServiceProvider? serviceProvider = null)
    {
        var dataCollector = new Collectors.AotTestDataCollector(metadataSource);
        var genericResolver = new Resolvers.AotGenericTypeResolver();
        var dynamicResolver = new Services.DataSourceResolver();
        var dataSourceExpander = new Expanders.DataSourceExpander(dynamicResolver);
        var testBuilder = new TestBuilder(serviceProvider);

        return new UnifiedTestBuilderPipeline(
            dataCollector,
            genericResolver,
            dataSourceExpander,
            testBuilder);
    }

    /// <summary>
    /// Creates a pipeline configured for reflection mode
    /// NOTE: Reflection mode has been removed in favor of AOT-only source generation.
    /// Use CreateAotPipeline instead.
    /// </summary>
    [Obsolete("Reflection mode has been removed for AOT compatibility. Use CreateAotPipeline instead.")]
    public static UnifiedTestBuilderPipeline CreateReflectionPipeline(
        Assembly[] assemblies,
        ITestInvoker testInvoker,
        IHookInvoker hookInvoker)
    {
        throw new NotSupportedException(
            "Reflection mode has been removed for AOT compatibility. Use CreateAotPipeline instead.");
    }
}
