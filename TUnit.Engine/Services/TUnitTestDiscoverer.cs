using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer(
    TestsConstructor testsConstructor,
    TestFilterService testFilterService,
    TestGrouper testGrouper,
    TestRegistrar testRegistrar,
    ITUnitMessageBus tUnitMessageBus,
    TUnitFrameworkLogger logger,
    TestsExecutor testsExecutor,
    IExtension extension) : IDataProducer
{
    private IReadOnlyCollection<DiscoveredTest>? _cachedTests;

    public IReadOnlyCollection<DiscoveredTest> GetTests(CancellationToken cancellationToken = default)
    {
        return _cachedTests ??= testsConstructor.GetTests(cancellationToken);
    }

    public async Task<GroupedTests> FilterTests(ExecuteRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
                
        var allDiscoveredTests = GetTests(cancellationToken);

        var executionRequest = context.Request as TestExecutionRequest;
        
        var filteredTests = testFilterService.FilterTests(executionRequest, allDiscoveredTests);
        
        await logger.LogTraceAsync($"Found {filteredTests.Count} tests after filtering.");
        
        var runOnTestDiscoveryTests = filteredTests
            .Where(x => x.TestContext.RunOnTestDiscovery)
            .ToArray();

        await testsExecutor.ExecuteAsync(new GroupedTests
        {
            AllValidTests = runOnTestDiscoveryTests,
            Parallel = runOnTestDiscoveryTests,
            ParallelGroups = [],
            NotInParallel = new PriorityQueue<DiscoveredTest, int>(),
            KeyedNotInParallel = new Dictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>>()
        }, executionRequest?.Filter, cancellationToken);
        
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