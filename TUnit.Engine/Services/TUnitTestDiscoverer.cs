using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
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
    TUnitFrameworkLogger logger,
    IExtension extension) : IDataProducer
{
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
        
        var filteredTests = testFilterService.FilterTests(executionRequest, allDiscoveredTests);
        
        await logger.LogTraceAsync($"Found {filteredTests.Count} tests after filtering.");
        
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
        
        var beforeDiscoveryHooks = testDiscoveryHookOrchestrator.CollectBeforeHooks();
        var beforeContext = testDiscoveryHookOrchestrator.GetBeforeContext();
        
        foreach (var beforeDiscoveryHook in beforeDiscoveryHooks)
        {
            if (beforeDiscoveryHook.IsSynchronous)
            {
                await logger.LogDebugAsync("Executing synchronous [Before(TestDiscovery)] hook");

                beforeDiscoveryHook.Execute(beforeContext, CancellationToken.None);
            }
            else
            {
                await logger.LogDebugAsync("Executing asynchronous [Before(TestDiscovery)] hook");

                await beforeDiscoveryHook.ExecuteAsync(beforeContext, CancellationToken.None);
            }
        }
        
        var allDiscoveredTests = testsConstructor.GetTests().ToArray();

        var afterDiscoveryHooks = testDiscoveryHookOrchestrator.CollectAfterHooks();
        var afterContext = testDiscoveryHookOrchestrator.GetAfterContext(allDiscoveredTests);
        
        foreach (var afterDiscoveryHook in afterDiscoveryHooks)
        {
            if (afterDiscoveryHook.IsSynchronous)
            {
                await logger.LogDebugAsync("Executing asynchronous [After(TestDiscovery)] hook");

                afterDiscoveryHook.Execute(afterContext, CancellationToken.None);
            }
            else
            {
                await logger.LogDebugAsync("Executing asynchronous [After(TestDiscovery)] hook");

                await afterDiscoveryHook.ExecuteAsync(afterContext, CancellationToken.None);
            }
        }        
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