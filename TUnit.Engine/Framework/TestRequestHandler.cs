using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Framework;

internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest:
                await HandleDiscoveryRequestAsync(serviceProvider, context);
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
        ExecuteRequestContext context)
    {
        try
        {
            await serviceProvider.HookOrchestrator.ExecuteBeforeTestDiscoveryHooksAsync(context.CancellationToken);

            var allTests = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value);

            foreach (var test in allTests)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                await serviceProvider.MessageBus.Discovered(test.Context);
            }
        }
        finally
        {
            await serviceProvider.HookOrchestrator.ExecuteAfterTestDiscoveryHooksAsync(context.CancellationToken);
        }
    }

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = (await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value)).ToArray();

        var filteredTests = serviceProvider.TestFilterService.FilterTests(request, allTests);

        // Report only the tests that will actually run
        foreach (var test in filteredTests)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await serviceProvider.MessageBus.Discovered(test.Context);
        }

        // Execute tests (executor will apply the same filter internally)
        await serviceProvider.TestExecutor.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}
