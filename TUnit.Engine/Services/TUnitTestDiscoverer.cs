using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Hooks;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer(
    HooksCollector hooksCollector,
    TestsConstructor testsConstructor,
    TestFilterService testFilterService,
    TestGrouper testGrouper,
    TestRegistrar testRegistrar,
    TestDiscoveryHookOrchestrator testDiscoveryHookOrchestrator,
    ITUnitMessageBus tUnitMessageBus,
    ILoggerFactory loggerFactory,
    IExtension extension) : IDataProducer
{
    private readonly ILogger<TUnitTestDiscoverer> _logger = loggerFactory.CreateLogger<TUnitTestDiscoverer>();
    
    private IReadOnlyCollection<DiscoveredTest>? _cachedTests;

    public IReadOnlyCollection<DiscoveredTest> GetCachedTests()
    {
        return _cachedTests!;
    }
    
    public async Task<GroupedTests> FilterTests(ExecuteRequestContext context, string? stringTestFilter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
                
        var allDiscoveredTests = _cachedTests ??= await DiscoverTests();

        var executionRequest = context.Request as TestExecutionRequest;
        
        var filteredTests = testFilterService.FilterTests(executionRequest?.Filter, allDiscoveredTests).ToArray();
        
        await _logger.LogTraceAsync($"Found {filteredTests.Length} tests after filtering.");
        
        var organisedTests = testGrouper.OrganiseTests(filteredTests);
        
        if (context.Request is TestExecutionRequest)
        {
            await RegisterInstances(organisedTests);
        }

        return organisedTests;
    }

    private async Task RegisterInstances(GroupedTests organisedTests)
    {
        foreach (var test in organisedTests.AllValidTests)
        {
            await testRegistrar.RegisterInstance(discoveredTest: test,
                onFailureToInitialize: exception => tUnitMessageBus.Failed(test.TestContext, exception, default)
            );
        }
    }

    private async Task<IReadOnlyCollection<DiscoveredTest>> DiscoverTests()
    {
        hooksCollector.CollectDiscoveryHooks();
        
        await testDiscoveryHookOrchestrator.ExecuteBeforeHooks();
        
        var allDiscoveredTests = testsConstructor.GetTests().ToArray();

        await testDiscoveryHookOrchestrator.ExecuteAfterHooks(allDiscoveredTests);
        
        hooksCollector.CollectHooks();

        return allDiscoveredTests;
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