using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Default implementation of request handler
/// </summary>
internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest discoverRequest:
                await HandleDiscoveryRequestAsync(serviceProvider, discoverRequest, context);
                break;

            case RunTestExecutionRequest runRequest:
                await HandleRunRequestAsync(serviceProvider, runRequest, context);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.GetType().Name,
                    "Unknown request type");
        }
    }

    private async Task HandleDiscoveryRequestAsync(
        TUnitServiceProvider serviceProvider,
        DiscoverTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();

        foreach (var test in allTests)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await serviceProvider.MessageBus.Discovered(test.Context!);
        }
    }

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();

        // Apply filter to tests before reporting discovery
        var testsToRun = allTests;
        if (request.Filter != null)
        {
            // Create a null logger factory for now - filtering will still work
            var loggerFactory = new NullLoggerFactory();
            var filterService = new TestFilterService(loggerFactory);
            testsToRun = filterService.FilterTests(request, allTests.ToArray()).ToList();
            
        }

        // Report only the tests that will actually run
        foreach (var test in testsToRun)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await serviceProvider.MessageBus.Discovered(test.Context!);
        }

        // Execute tests (executor will apply the same filter internally)
        await serviceProvider.TestExecutor.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}