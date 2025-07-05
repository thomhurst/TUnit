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
            ClassMetadata = CreateClassMetadata(metadata),
            MethodMetadata = CreateMethodMetadata(metadata)
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
                Status = Status.Failed,
                Start = DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = Environment.MachineName,
                TestContext = context
            }
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ClassMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    private static ClassMetadata CreateClassMetadata(TestMetadata metadata)
    {
        var type = metadata.TestClassType;

        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => new ClassMetadata
        {
            Name = type.Name,
            Type = type,
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Namespace = type.Namespace,
            Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata
            {
                Name = type.Assembly.GetName().Name ?? "Unknown",
                Attributes = []
            }),
            Parameters = [],
            Properties = [],
            Parent = null,
            Attributes = []
        });
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.MethodMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known at compile time")]
    private static MethodMetadata CreateMethodMetadata(TestMetadata metadata)
    {
        // In AOT mode, use metadata directly without reflection
        // Create parameters from ParameterTypes array
        var parameters = metadata.ParameterTypes.Select((type, index) => new ParameterMetadata(type)
        {
            Name = $"param{index}",
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Attributes = [],
            ReflectionInfo = null! // No reflection info in AOT mode
        }).ToArray();

        // Minimal metadata when MethodInfo is not available
        return new MethodMetadata
        {
            Name = metadata.TestMethodName,
            Type = metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(metadata.TestClassType.AssemblyQualifiedName ?? metadata.TestClassType.FullName ?? metadata.TestClassType.Name),
            Class = CreateClassMetadata(metadata),
            Parameters = parameters,
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
            ReturnType = typeof(Task),
            Attributes = []
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
        var dataSourceExpander = new Expanders.DataSourceExpander();
        var testBuilder = new TestBuilder(serviceProvider);

        return new UnifiedTestBuilderPipeline(
            dataCollector,
            genericResolver,
            dataSourceExpander,
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
        IServiceProvider? serviceProvider = null,
        Assembly[]? assembliesToScan = null)
    {
        var dataCollector = await TestDataCollectorFactory.CreateAutoDetectAsync(assembliesToScan);
        var genericResolver = new Resolvers.AotGenericTypeResolver();
        var dataSourceExpander = new Expanders.DataSourceExpander();
        var testBuilder = new TestBuilder(serviceProvider);

        return new UnifiedTestBuilderPipeline(
            dataCollector,
            genericResolver,
            dataSourceExpander,
            testBuilder);
    }
}
