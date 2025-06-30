using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Building;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Simplified test executor that works directly with ExecutableTest
/// </summary>
public sealed class UnifiedTestExecutor : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly ILoggerFactory _loggerFactory;
    private SessionUid? _sessionUid;
    private readonly CancellationTokenSource _failFastCancellationSource = new();

    public UnifiedTestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ILoggerFactory? loggerFactory = null,
        ITestScheduler? testScheduler = null)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();

        // Use provided scheduler or create default
        _testScheduler = testScheduler ?? CreateDefaultScheduler();
    }

    // IDataProducer implementation
    public string Uid => "TUnit.UnifiedTestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Executor";
    public string Description => "Unified test executor for TUnit";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    /// <summary>
    /// Sets the session ID for test reporting
    /// </summary>
    public void SetSessionId(SessionUid sessionUid)
    {
        _sessionUid = sessionUid;
    }

    /// <summary>
    /// Executes a collection of tests with proper parallelization and dependency handling
    /// </summary>
    public async Task ExecuteTests(
        IEnumerable<ExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();

        // Apply filter if provided
        if (filter != null)
        {
            var beforeCount = testList.Count;
            testList = ApplyFilter(testList, filter);
        }

        // Check if fail-fast is enabled
        var isFailFastEnabled = IsFailFastEnabled();

        // Create executor adapter with fail-fast support
        var executorAdapter = new FailFastTestExecutorAdapter(
            _singleTestExecutor,
            messageBus,
            _sessionUid ?? new SessionUid(Guid.NewGuid().ToString()),
            isFailFastEnabled,
            _failFastCancellationSource,
            _logger);

        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _failFastCancellationSource.Token);

        // Schedule and execute tests
        await _testScheduler.ScheduleAndExecuteAsync(testList, executorAdapter, linkedCts.Token);
    }

    private bool IsFailFastEnabled()
    {
        return _commandLineOptions.TryGetOptionArgumentList(
            FailFastCommandProvider.FailFast,
            out _);
    }

    private ITestScheduler CreateDefaultScheduler()
    {
        return TestSchedulerFactory.CreateDefault(_logger);
    }

    private List<ExecutableTest> ApplyFilter(List<ExecutableTest> tests, ITestExecutionFilter filter)
    {
        // Debug: Applying filter to {tests.Count} tests of type {filter.GetType().Name}

        // Use TestFilterService to apply the filter
        var filterService = new TestFilterService(_loggerFactory);

        // Filter tests directly - TestFilterService handles the request internally
        var filteredTests = new List<ExecutableTest>();
        foreach (var test in tests)
        {
            if (filterService.MatchesTest(filter, test))
            {
                filteredTests.Add(test);
            }
        }

        // Debug: Filter matched {filteredTests.Count} tests

        return filteredTests;
    }



    /// <summary>
    /// Implementation of ITestExecutor for Microsoft.Testing.Platform
    /// </summary>
    public async Task ExecuteAsync(
        RunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // AOT-only mode: Always use source generation
        await ExecuteAsyncAotSafe(request, messageBus, cancellationToken);
    }

    /// <summary>
    /// AOT-safe test execution (source generation path)
    /// </summary>
    private async Task ExecuteAsyncAotSafe(
        RunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Create unified pipeline for AOT mode
        var sources = TestMetadataRegistry.GetSources();
        var metadataSource = new SourceGeneratedTestMetadataSource(() =>
            sources.SelectMany(s => s.GetTestMetadata().GetAwaiter().GetResult()).ToList());

        var pipeline = UnifiedTestBuilderPipelineFactory.CreateAotPipeline(
            metadataSource,
            new TestInvoker(),
            new HookInvoker());

        var discoveryService = new TestDiscoveryServiceV2(pipeline, enableDynamicDiscovery: false);
        var tests = await discoveryService.DiscoverTests();

        // Execute tests
        await ExecuteTests(tests, request.Filter, messageBus, cancellationToken);
    }

    /// <summary>
    /// Reflection-based test execution (OBSOLETE - throws NotSupportedException)
    /// </summary>
    [Obsolete("Reflection mode has been removed for AOT compatibility. Use ExecuteAsyncWithSourceGeneration instead.")]
    [RequiresDynamicCode("Generic type resolution requires runtime type generation.")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming.")]
    private Task ExecuteAsyncWithReflection(
        RunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Reflection mode has been removed for AOT compatibility. Use source generation mode only.");
    }

}
