using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Framework;

internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, ITestDiscoveryServices discoveryServices, ITestExecutionServices executionServices, ILoggingServices loggingServices, ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest:
                await HandleDiscoveryRequestAsync(discoveryServices, loggingServices, context, testExecutionFilter);
                break;

            case RunTestExecutionRequest runRequest:
                await HandleRunRequestAsync(discoveryServices, executionServices, runRequest, context, testExecutionFilter);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.GetType().Name,
                    "Unknown request type");
        }
    }

    private async Task HandleDiscoveryRequestAsync(
        ITestDiscoveryServices discoveryServices,
        ILoggingServices loggingServices,
        ExecuteRequestContext context,
        ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await discoveryServices.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: false);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        foreach (var test in discoveryResult.Tests)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            await loggingServices.MessageBus.Discovered(test.Context);
        }
    }

    private async Task HandleRunRequestAsync(
        ITestDiscoveryServices discoveryServices,
        ITestExecutionServices executionServices,
        RunTestExecutionRequest request,
        ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await discoveryServices.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: true);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        var allTests = discoveryResult.Tests.ToArray();

        // Skip sending Discovered messages during execution - they're only needed for discovery requests
        // This saves significant time and allocations when running tests

        await executionServices.TestSessionCoordinator.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}
