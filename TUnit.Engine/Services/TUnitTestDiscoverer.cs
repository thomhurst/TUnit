﻿using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Hooks;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TUnitTestDiscoverer(
    TestsConstructor testsConstructor,
    TestFilterService testFilterService,
    TestGrouper testGrouper,
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
        GlobalContext.Current.TestFilter = stringTestFilter;

        cancellationToken.ThrowIfCancellationRequested();
                
        var allDiscoveredTests = _cachedTests ??= await DiscoverTests(stringTestFilter);

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
            await TestRegistrar.RegisterInstance(testContext: test.TestContext,
                onFailureToInitialize: exception => tUnitMessageBus.Errored(test.TestContext, exception)
            );
        }
    }

    private async Task<IReadOnlyCollection<DiscoveredTest>> DiscoverTests(string? stringTestFilter)
    {
        await TestDiscoveryHookOrchestrator.ExecuteBeforeHooks(new BeforeTestDiscoveryContext
        {
            TestFilter = stringTestFilter
        });
        
        var allDiscoveredTests = testsConstructor.GetTests().ToArray();

        await TestDiscoveryHookOrchestrator.ExecuteAfterHooks(
            new TestDiscoveryContext(allDiscoveredTests)
            {
                TestFilter = stringTestFilter
            });
        
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