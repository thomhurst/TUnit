using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Hooks;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer(
    TestsCollector testsCollector,
    TestFilterService testFilterService,
    TestGrouper testGrouper,
    ILoggerFactory loggerFactory,
    IExtension extension) : IDataProducer
{
    private readonly ILogger<TUnitTestDiscoverer> _logger = loggerFactory.CreateLogger<TUnitTestDiscoverer>();
    
    private IReadOnlyCollection<DiscoveredTest>? _cachedTests;
    
    public async Task<GroupedTests> FilterTests(ExecuteRequestContext context, string? stringTestFilter, CancellationToken cancellationToken)
    {
        GlobalContext.Current.TestFilter = stringTestFilter;

        cancellationToken.ThrowIfCancellationRequested();
                
        var allDiscoveredTests = _cachedTests ??= await DiscoverTests(context, stringTestFilter);

        var executionRequest = context.Request as TestExecutionRequest;
        
        var filteredTests = testFilterService.FilterTests(executionRequest?.Filter, allDiscoveredTests).ToArray();
        
        await _logger.LogTraceAsync($"Found {filteredTests.Length} tests after filtering.");
        
        var organisedTests = testGrouper.OrganiseTests(filteredTests, GetFailedToInitializeTests());
        
        if (context.Request is TestExecutionRequest)
        {
            await RegisterInstances(context, organisedTests);
        }

        return organisedTests;
    }

    private async Task RegisterInstances(ExecuteRequestContext context, GroupedTests organisedTests)
    {
        foreach (var test in organisedTests.AllValidTests)
        {
            await TestRegistrar.RegisterInstance(testContext: test.TestContext,

                onFailureToInitialize: exception => context.MessageBus.PublishAsync(
                    dataProducer: this,
                    data: new TestNodeUpdateMessage(
                        sessionUid: context.Request.Session.SessionUid,
                        testNode: test.TestContext
                            .ToTestNode()
                            .WithProperty(new ErrorTestNodeStateProperty(exception, "Error initializing test")
                            )
                    )
                )
            );
        }
    }

    private async Task<IReadOnlyCollection<DiscoveredTest>> DiscoverTests(ExecuteRequestContext context, string? stringTestFilter)
    {
        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(new BeforeTestDiscoveryContext
        {
            TestFilter = stringTestFilter
        });
        
        var allDiscoveredTests = testsCollector.GetTests(context).ToArray();

        await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(
            new TestDiscoveryContext(allDiscoveredTests)
            {
                TestFilter = stringTestFilter
            });
        
        return allDiscoveredTests;
    }

    private FailedInitializationTest[] GetFailedToInitializeTests()
    {
        var failedToInitializeTests = TestDictionary.GetFailedToInitializeTests();

        _logger.LogWarning($"{failedToInitializeTests.Length} tests failed to initialize.");
        
        return failedToInitializeTests;
    }

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
}