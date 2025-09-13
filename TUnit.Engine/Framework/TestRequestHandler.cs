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
        
        // Check if we have only skipped tests
        // When all tests are skipped, the run should be marked as failed per TUnit requirements
        var hasAnyPassedTest = false;
        var hasAnySkippedTest = false;
        var hasAnyFailedTest = false;
        
        foreach (var test in allTests)
        {
            if (test.Result?.State == TestState.Passed)
            {
                hasAnyPassedTest = true;
            }
            else if (test.Result?.State == TestState.Skipped)
            {
                hasAnySkippedTest = true;
            }
            else if (test.Result?.State == TestState.Failed)
            {
                hasAnyFailedTest = true;
            }
        }
        
        // If we have no passed tests and no failed tests, but we have skipped tests,
        // send a failed test node to force the overall outcome to be "Failed"
        if (!hasAnyPassedTest && !hasAnyFailedTest && hasAnySkippedTest)
        {
            // Create a synthetic failed test to mark the run as failed
            await context.MessageBus.PublishAsync(
                dataProducer: serviceProvider.MessageBus,
                data: new TestNodeUpdateMessage(
                    sessionUid: context.Request.Session.SessionUid,
                    testNode: new TestNode
                    {
                        DisplayName = "Test run incomplete",
                        Uid = new TestNodeUid($"skipped-tests-marker-{Guid.NewGuid()}"),
                        Properties = new PropertyBag(new FailedTestNodeStateProperty(
                            new Exception("All tests were skipped - marking run as failed")))
                    }));
        }
    }
}
