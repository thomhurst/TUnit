using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Core.Tracking;
using TUnit.Engine.Extensions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// Handles ExecutionContext restoration for AsyncLocal support and test lifecycle management
internal class TestOrchestrator : ITestOrchestrator
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestResultFactory _resultFactory;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly EngineCancellationToken _engineCancellationToken;
    private readonly TestExecutor _testExecutor;
    private SessionUid _sessionUid;

    public TestOrchestrator(TUnitFrameworkLogger logger,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        EngineCancellationToken engineCancellationToken,
        TestExecutor testExecutor,
        SessionUid sessionUid)
    {
        _logger = logger;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _engineCancellationToken = engineCancellationToken;
        _testExecutor = testExecutor;
        _sessionUid = sessionUid;
        _resultFactory = new TestResultFactory();
    }

    public void SetSessionId(SessionUid sessionUid)
    {
        _sessionUid = sessionUid;
    }

    public async Task<TestNodeUpdateMessage> ExecuteTestAsync(
        AbstractExecutableTest test,
        CancellationToken cancellationToken)
    {
        await ExecuteTestInternalAsync(test, cancellationToken).ConfigureAwait(false);

        if (test.State == TestState.Running)
        {
            test.State = TestState.Failed;
            test.Result ??= new TestResult
            {
                State = TestState.Failed,
                Start = test.StartTime ?? DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = new InvalidOperationException($"Test execution completed but state was not updated properly"),
                ComputerName = Environment.MachineName,
                TestContext = test.Context
            };
        }

        return CreateUpdateMessage(test);
    }

    private async Task<TestResult> ExecuteTestInternalAsync(
        AbstractExecutableTest test,
        CancellationToken cancellationToken)
    {
        try
        {
            if (test is { State: TestState.Failed, Result: not null })
            {
                return test.Result;
            }

            TestContext.Current = test.Context;

            test.StartTime = DateTimeOffset.Now;
            test.State = TestState.Running;

            if (!string.IsNullOrEmpty(test.Context.SkipReason))
            {
                return await HandleSkippedTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
            }

            if (test.Context.TestDetails.ClassInstance is SkippedTestInstance)
            {
                return await HandleSkippedTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
            }

            if (test.Context.TestDetails.ClassInstance is PlaceholderInstance)
            {
                var createdInstance = await test.CreateInstanceAsync().ConfigureAwait(false);
                if (createdInstance == null)
                {
                    throw new InvalidOperationException($"CreateInstanceAsync returned null for test {test.Context.GetDisplayName()}. This is likely a framework bug.");
                }
                test.Context.TestDetails.ClassInstance = createdInstance;
            }

            var instance = test.Context.TestDetails.ClassInstance;

            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Test instance is null for test {test.Context.GetDisplayName()} after instance creation. ClassInstance type: {test.Context.TestDetails.ClassInstance?.GetType()?.Name ?? "null"}");
            }

            if (instance is PlaceholderInstance)
            {
                throw new InvalidOperationException($"Test instance is still PlaceholderInstance for test {test.Context.GetDisplayName()}. This should have been replaced.");
            }

            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.ClassArguments, test.Context.ObjectBag, test.Context.TestDetails.MethodMetadata,
                test.Context.Events).ConfigureAwait(false);

            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.Arguments, test.Context.ObjectBag, test.Context.TestDetails.MethodMetadata,
                test.Context.Events).ConfigureAwait(false);

            await PropertyInjectionService.InjectPropertiesAsync(
                test.Context,
                instance,
                test.Metadata.PropertyDataSources,
                test.Metadata.PropertyInjections,
                test.Metadata.MethodMetadata,
                test.Context.TestDetails.TestId).ConfigureAwait(false);

            if (instance is IAsyncDisposable or IDisposable)
            {
                ObjectTracker.TrackObject(test.Context.Events, instance);
            }

            // Inject properties into test attributes BEFORE they are initialized
            // This ensures that data source generators and other attributes have their dependencies ready
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(
                test.Context.TestDetails.Attributes.ToArray(),
                test.Context.ObjectBag,
                test.Context.TestDetails.MethodMetadata,
                test.Context.Events).ConfigureAwait(false);

            await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken).ConfigureAwait(false);

            PopulateTestContextDependencies(test);

            CheckDependenciesAndThrowIfShouldSkip(test);

            // Still need to invoke test start event receivers directly (this is test-specific, not lifecycle)
            await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);

            try
            {
                if (!string.IsNullOrEmpty(test.Context.SkipReason))
                {
                    return await HandleSkippedTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
                }

                if (test.Context is { RetryFunc: not null, TestDetails.RetryLimit: > 0 })
                {
                    await ExecuteTestWithRetries(() => _testExecutor.ExecuteAsync(test, cancellationToken), test.Context, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _testExecutor.ExecuteAsync(test, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (TestDependencyException e)
            {
                test.Context.SkipReason = e.Message;
                return await HandleSkippedTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
            }
            catch (SkipTestException e)
            {
                test.Context.SkipReason = e.Reason;
                return await HandleSkippedTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (_engineCancellationToken.Token.IsCancellationRequested && exception is OperationCanceledException or TaskCanceledException)
            {
                HandleCancellation(test);
            }
            catch (Exception ex)
            {
                HandleTestFailure(test, ex);
            }
            finally
            {
                test.EndTime = DateTimeOffset.Now;

                await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context!, cancellationToken).ConfigureAwait(false);

                // Trigger disposal events for tracked objects after test completion
                // Disposal order: Objects are disposed in ascending order (lower Order values first)
                // This ensures dependencies are disposed before their dependents
                await TriggerDisposalEventsAsync(test.Context, "test context objects").ConfigureAwait(false);
            }

            if (test.Result == null)
            {
                test.State = TestState.Failed;
                test.Result = new TestResult
                {
                    State = TestState.Failed,
                    Start = test.StartTime ?? DateTimeOffset.UtcNow,
                    End = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.Zero,
                    Exception = new InvalidOperationException("Test execution completed but no result was set"),
                    ComputerName = Environment.MachineName,
                    TestContext = test.Context
                };
            }
            return test.Result;
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.EndTime = DateTimeOffset.Now;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Start = test.StartTime ?? DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Exception = ex,
                ComputerName = Environment.MachineName,
                TestContext = test.Context
            };
            return test.Result;
        }
    }

    private async Task ExecuteTestWithRetries(Func<Task> testDelegate, TestContext testContext, CancellationToken cancellationToken)
    {
        var retryLimit = testContext.TestDetails.RetryLimit;
        var retryFunc = testContext.RetryFunc!;

        for (var i = 0; i < retryLimit + 1; i++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await testDelegate().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (i < retryLimit)
            {
                if (!await retryFunc(testContext, ex, i + 1).ConfigureAwait(false))
                {
                    throw;
                }

                await _logger.LogWarningAsync($"Retrying test due to exception: {ex.Message}. Attempt {i} of {retryLimit}.").ConfigureAwait(false);
            }
        }
    }

    private async Task<TestResult> HandleSkippedTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Skipped;

        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Context.SkipReason ?? "Test skipped");

        test.EndTime = DateTimeOffset.Now;
        await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);

        var instance = test.Context.TestDetails.ClassInstance;
        if (instance != null &&
            instance is not SkippedTestInstance &&
            instance is not PlaceholderInstance)
        {
            if (instance is IAsyncDisposable or IDisposable)
            {
                ObjectTracker.TrackObject(test.Context.Events, instance);
            }
        }

        // Trigger disposal events for tracked objects after skipped test processing
        // Disposal order: Objects are disposed in ascending order (lower Order values first)
        // This ensures dependencies are disposed before their dependents
        await TriggerDisposalEventsAsync(test.Context, "skipped test context objects").ConfigureAwait(false);

        return test.Result;
    }


    private void HandleTestFailure(AbstractExecutableTest test, Exception ex)
    {
        if (ex is OperationCanceledException && test.Context.TestDetails.Timeout.HasValue)
        {
            test.State = TestState.Timeout;
            test.Result = _resultFactory.CreateTimeoutResult(
                test.StartTime!.Value,
                (int)test.Context.TestDetails.Timeout.Value.TotalMilliseconds);
        }
        else
        {
            test.State = TestState.Failed;
            test.Result = _resultFactory.CreateFailedResult(
                test.StartTime!.Value,
                ex);
        }
    }

    private void HandleCancellation(AbstractExecutableTest test)
    {
        test.State = TestState.Cancelled;
        test.Result = _resultFactory.CreateCancelledResult(test.StartTime!.Value);
    }

    private TestNodeUpdateMessage CreateUpdateMessage(AbstractExecutableTest test)
    {
        var testNode = test.Context.ToTestNode()
            .WithProperty(GetTestNodeState(test));

        var standardOutput = test.Context.GetStandardOutput();
        var errorOutput = test.Context.GetErrorOutput();

        if (!string.IsNullOrEmpty(standardOutput))
        {
#pragma warning disable TPEXP
            testNode = testNode.WithProperty(new StandardOutputProperty(standardOutput));
#pragma warning restore TPEXP
        }

        if (!string.IsNullOrEmpty(errorOutput))
        {
#pragma warning disable TPEXP
            testNode = testNode.WithProperty(new StandardErrorProperty(errorOutput));
#pragma warning restore TPEXP
        }

        return new TestNodeUpdateMessage(
            sessionUid: _sessionUid,
            testNode: testNode);
    }

    private IProperty GetTestNodeState(AbstractExecutableTest test)
    {
        return test.State switch
        {
            TestState.Passed => PassedTestNodeStateProperty.CachedInstance,
            TestState.Failed => new FailedTestNodeStateProperty(test.Result?.Exception ?? new InvalidOperationException($"Test failed but no exception was provided for {test.Context.GetDisplayName()}")),
            TestState.Skipped => new SkippedTestNodeStateProperty(test.Result?.OverrideReason ?? test.Context.SkipReason ?? "Test skipped"),
            TestState.Timeout => new TimeoutTestNodeStateProperty(test.Result?.OverrideReason ?? "Test timed out"),
            TestState.Cancelled => new CancelledTestNodeStateProperty(),
            TestState.Running => new FailedTestNodeStateProperty(new InvalidOperationException($"Test is still running: {test.Context.TestDetails.ClassType.FullName}.{test.Context.GetDisplayName()}")),
            _ => new FailedTestNodeStateProperty(new InvalidOperationException($"Unknown test state: {test.State}"))
        };
    }


    private void PopulateTestContextDependencies(AbstractExecutableTest test)
    {
        test.Context.Dependencies.Clear();
        var allDependencies = new HashSet<TestDetails>();
        CollectTransitiveDependencies(test, allDependencies, [
        ]);
        test.Context.Dependencies.AddRange(allDependencies);
    }

    private void CollectTransitiveDependencies(AbstractExecutableTest test, HashSet<TestDetails> collected, HashSet<AbstractExecutableTest> visited)
    {
        if (!visited.Add(test))
        {
            return;
        }

        foreach (var resolvedDependency in test.Dependencies)
        {
            var dependencyTest = resolvedDependency.Test;
            if (dependencyTest.Context?.TestDetails != null)
            {
                collected.Add(dependencyTest.Context.TestDetails);

                CollectTransitiveDependencies(dependencyTest, collected, visited);
            }
        }
    }

    private void CheckDependenciesAndThrowIfShouldSkip(AbstractExecutableTest test)
    {
        var failedDependenciesNotAllowingProceed = new List<string>();

        foreach (var dependency in test.Dependencies)
        {
            if (dependency.Test.State == TestState.Failed || dependency.Test.State == TestState.Timeout)
            {
                if (!dependency.ProceedOnFailure)
                {
                    var dependencyName = GetDependencyDisplayName(dependency.Test);
                    failedDependenciesNotAllowingProceed.Add(dependencyName);
                }
            }
        }

        if (failedDependenciesNotAllowingProceed.Count > 0)
        {
            var dependencyNames = string.Join(", ", failedDependenciesNotAllowingProceed);
            throw new TestDependencyException(dependencyNames, false);
        }
    }

    private string GetDependencyDisplayName(AbstractExecutableTest dependency)
    {
        return dependency.Context?.GetDisplayName() ?? $"{dependency.Context?.TestDetails.ClassType.Name}.{dependency.Context?.TestDetails.TestName}" ?? "Unknown";
    }

    /// <summary>
    /// Triggers disposal events for tracked objects with proper error handling and aggregation.
    /// Disposal order: Objects are disposed in ascending order (lower Order values first)
    /// to ensure dependencies are disposed before their dependents.
    /// </summary>
    /// <param name="context">The test context containing disposal events</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    private async Task TriggerDisposalEventsAsync(TestContext context, string operationName)
    {
        if (context.Events.OnDispose == null)
        {
            return;
        }

        var disposalExceptions = new List<Exception>();

        try
        {
            // Dispose objects in order - lower Order values first to handle dependencies correctly
            var orderedInvocations = context.Events.OnDispose.InvocationList.OrderBy(x => x.Order);

            foreach (var invocation in orderedInvocations)
            {
                try
                {
                    await invocation.InvokeAsync(context, context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Collect disposal exceptions but continue disposing other objects
                    disposalExceptions.Add(ex);
                }
            }
        }
        catch (Exception ex)
        {
            // Catch any unexpected errors in the disposal process itself
            disposalExceptions.Add(ex);
        }

        // Log disposal errors without failing the test
        if (disposalExceptions.Count > 0)
        {
            if (disposalExceptions.Count == 1)
            {
                await _logger.LogErrorAsync($"Error during {operationName}: {disposalExceptions[0].Message}").ConfigureAwait(false);
            }
            else
            {
                var aggregateMessage = string.Join("; ", disposalExceptions.Select(ex => ex.Message));
                await _logger.LogErrorAsync($"Multiple errors during {operationName}: {aggregateMessage}").ConfigureAwait(false);
            }
        }
    }
}
