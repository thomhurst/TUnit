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
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, GetFilter(context), context.CancellationToken);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        foreach (var test in discoveryResult.Tests)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            await serviceProvider.MessageBus.Discovered(test.Context);
        }
    }

    private ITestExecutionFilter? GetFilter(ExecuteRequestContext context)
    {
        if (context.Request is RunTestExecutionRequest runRequest)
        {
            return runRequest.Filter;
        }

        if (context.Request is DiscoverTestExecutionRequest discoverTestExecutionRequest)
        {
            return discoverTestExecutionRequest.Filter;
        }

        return null;
    }

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, GetFilter(context), context.CancellationToken);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        var allTests = discoveryResult.Tests.ToArray();

        // Report only the tests that will actually run
        foreach (var test in allTests)
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
