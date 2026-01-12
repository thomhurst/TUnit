using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Framework;

internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest:
                await HandleDiscoveryRequestAsync(serviceProvider, context, testExecutionFilter);
                break;

            case RunTestExecutionRequest runRequest:
                await HandleRunRequestAsync(serviceProvider, runRequest, context, testExecutionFilter);
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
        ExecuteRequestContext context,
        ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: false);

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

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: true);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        var allTests = discoveryResult.Tests.ToArray();

        // Skip sending Discovered messages during execution - they're only needed for discovery requests
        // This saves significant time and allocations when running tests

        await serviceProvider.TestSessionCoordinator.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}
