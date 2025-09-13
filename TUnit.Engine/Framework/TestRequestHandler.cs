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
        // For discovery, we want to show ALL tests including explicit ones
        // Pass isForExecution: false to discover all tests - filtering is only for execution
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
        // For execution, apply filtering to exclude explicit tests unless explicitly targeted
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: true);

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

        // Execute tests
        await serviceProvider.TestSessionCoordinator.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
        
        // Check if we have only skipped tests (no passed tests) after execution
        // The TRX reporter needs this to mark the run as "Failed" instead of "Completed"
        var skippedCount = 0;
        var passedCount = 0;
        var failedCount = 0;
        var notExecutedCount = 0;
        
        foreach (var test in allTests)
        {
            switch (test.State)
            {
                case TestState.Skipped:
                    skippedCount++;
                    break;
                case TestState.Passed:
                    passedCount++;
                    break;
                case TestState.Failed:
                    failedCount++;
                    break;
                case TestState.NotStarted:
                    // Tests that weren't executed at all (e.g., due to skip attributes)
                    notExecutedCount++;
                    break;
            }
        }
        
        // When we have tests that were not executed (skipped or not started) and no passed tests,
        // the run should be considered failed
        if ((skippedCount > 0 || notExecutedCount > 0) && passedCount == 0 && failedCount == 0)
        {
            // Send an error message to signal this to the TRX reporter
            await context.MessageBus.PublishAsync(
                dataProducer: serviceProvider.MessageBus,
                data: new TestNodeUpdateMessage(
                    sessionUid: context.Request.Session.SessionUid,
                    testNode: new TestNode
                    {
                        DisplayName = "Test run incomplete - all tests were skipped",
                        Uid = new TestNodeUid($"skipped-tests-error-{Guid.NewGuid()}"),
                        Properties = new PropertyBag(new ErrorTestNodeStateProperty(
                            new Exception("Test run should be marked as failed when all tests are skipped")))
                    }));
        }
    }
}
