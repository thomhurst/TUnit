using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.ReferenceTracking;
using TUnit.Engine.Extensions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Services;

/// Handles ExecutionContext restoration for AsyncLocal support and test lifecycle management
internal class SingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestResultFactory _resultFactory;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private SessionUid? _sessionUid;

    public SingleTestExecutor(TUnitFrameworkLogger logger, EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _logger = logger;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _resultFactory = new TestResultFactory();
    }

    public void SetSessionId(SessionUid sessionUid)
    {
        _sessionUid = sessionUid;
    }

    public async Task<TestNodeUpdateMessage> ExecuteTestAsync(
        ExecutableTest test,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // If test is already failed (e.g., from data source expansion error),
        // just report the existing failure
        if (test is { State: TestState.Failed, Result: not null })
        {
            return CreateUpdateMessage(test);
        }

        test.StartTime = DateTimeOffset.Now;
        test.State = TestState.Running;

        // Create the test instance early so it's available for property injection
        var instance = await test.CreateInstanceAsync();
        test.Context.TestDetails.ClassInstance = instance;

        // Now inject properties with the instance available
        await PropertyInjector.InjectPropertiesAsync(
            test.Context,
            instance,
            test.Metadata.PropertyDataSources,
            test.Metadata.PropertyInjections,
            test.Metadata.MethodMetadata,
            test.Context.TestDetails.TestId);

        // Initialize all eligible objects before test starts
        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken);

        // Invoke first test event receivers if this is the first test
        var classContext = test.Context.ClassContext;
        var assemblyContext = classContext.AssemblyContext;
        var sessionContext = assemblyContext.TestSessionContext;

        // First test in session
        await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(test.Context, sessionContext, cancellationToken);

        // First test in assembly
        await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(test.Context, assemblyContext, cancellationToken);

        // First test in class
        await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(test.Context, classContext, cancellationToken);

        // Invoke test start event receivers
        await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(test.Context, cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(test.Context.SkipReason))
            {
                return await HandleSkippedTestAsync(test, cancellationToken);
            }

            await ExecuteTestWithHooksAsync(test, instance, cancellationToken);
        }
        catch (Exception ex)
        {
            HandleTestFailure(test, ex);
        }
        finally
        {
            test.EndTime = DateTimeOffset.Now;

            // Release data source references
            await ReleaseDataSourceReferences(test.Context!);

            // Invoke test end event receivers
            await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context!, cancellationToken);

            // Invoke last test event receivers if this is the last test
            var endClassContext = test.Context!.ClassContext;
            var endAssemblyContext = endClassContext.AssemblyContext;
            var endSessionContext = endAssemblyContext.TestSessionContext;

            // Last test in class
            await _eventReceiverOrchestrator.InvokeLastTestInClassEventReceiversAsync(test.Context, endClassContext, cancellationToken);

            // Last test in assembly
            await _eventReceiverOrchestrator.InvokeLastTestInAssemblyEventReceiversAsync(test.Context, endAssemblyContext, cancellationToken);

            // Last test in session
            await _eventReceiverOrchestrator.InvokeLastTestInSessionEventReceiversAsync(test.Context, endSessionContext, cancellationToken);
        }

        return CreateUpdateMessage(test);
    }

    private async Task<TestNodeUpdateMessage> HandleSkippedTestAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Skipped;
        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Context?.SkipReason ?? "Test skipped");
        test.EndTime = DateTimeOffset.Now;

        // Invoke test skipped event receivers
        await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context!, cancellationToken);

        return CreateUpdateMessage(test);
    }

    private async Task ExecuteTestWithHooksAsync(ExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        // Instance is already created and properties are already injected
        // Restore hook contexts before test execution
        RestoreHookContexts(test.Context);

        try
        {
            await ExecuteBeforeTestHooksAsync(test.BeforeTestHooks, test.Context, cancellationToken);

            // Restore the test context execution context to make AsyncLocal values available
            test.Context.RestoreExecutionContext();

            await InvokeTestWithTimeout(test, instance, cancellationToken);

            test.State = TestState.Passed;
            test.Result = _resultFactory.CreatePassedResult(test.StartTime!.Value);
        }
        catch (Exception ex)
        {
            HandleTestFailure(test, ex);
            throw;
        }
        finally
        {
            await ExecuteAfterTestHooksAsync(test.AfterTestHooks, test.Context, cancellationToken);
            await DecrementAndDisposeTrackedObjectsAsync(test);
        }
    }


    private async Task ExecuteBeforeTestHooksAsync(Func<TestContext, CancellationToken, Task>[] hooks, TestContext context, CancellationToken cancellationToken)
    {
        // Restore initial context before running hooks
        RestoreHookContexts(context);
        context.RestoreExecutionContext();

        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);

                // After each hook, restore the context to ensure any AsyncLocal values
                // set by the hook (via AddAsyncLocalValues) are available to subsequent hooks
                context.RestoreExecutionContext();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in before test hook: {ex.Message}");
                throw;
            }
        }
    }

    private async Task ExecuteAfterTestHooksAsync(Func<TestContext, CancellationToken, Task>[] hooks, TestContext context, CancellationToken cancellationToken)
    {
        foreach (var hook in hooks)
        {
            try
            {
                // Restore hook contexts before executing the hook
                RestoreHookContexts(context);

                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in after test hook: {ex.Message}");
            }
        }
    }

    private void HandleTestFailure(ExecutableTest test, Exception ex)
    {
        if (ex is OperationCanceledException && test.Metadata.TimeoutMs.HasValue)
        {
            test.State = TestState.Timeout;
            test.Result = _resultFactory.CreateTimeoutResult(
                test.StartTime!.Value,
                test.Metadata.TimeoutMs.Value);
        }
        else
        {
            test.State = TestState.Failed;
            test.Result = _resultFactory.CreateFailedResult(
                test.StartTime!.Value,
                ex);
        }
    }

    private async Task ReleaseDataSourceReferences(TestContext context)
    {
        var testDetails = context.TestDetails;

        // Release test method arguments
        foreach (var arg in testDetails.TestMethodArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(arg);
        }

        // Release test class constructor arguments
        foreach (var arg in testDetails.TestClassArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(arg);
        }

        // Release injected property values
        foreach (var kvp in testDetails.TestClassInjectedPropertyArguments)
        {
            await DataSourceReferenceTrackerProvider.ReleaseDataSourceObject(kvp.Value);
        }
    }

    private TestNodeUpdateMessage CreateUpdateMessage(ExecutableTest test)
    {
        var testNode = test.Context.ToTestNode()
            .WithProperty(GetTestNodeState(test));

        // Include captured console output
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

        var sessionUid = _sessionUid ?? CreateSessionUid(test);

        return new TestNodeUpdateMessage(
            sessionUid: sessionUid,
            testNode: testNode);
    }

    private IProperty GetTestNodeState(ExecutableTest test)
    {
        return test.State switch
        {
            TestState.Passed => PassedTestNodeStateProperty.CachedInstance,
            TestState.Failed => new FailedTestNodeStateProperty(test.Result!.Exception!),
            TestState.Skipped => new SkippedTestNodeStateProperty(test.Result!.OverrideReason ?? "Test skipped"),
            TestState.Timeout => new TimeoutTestNodeStateProperty(test.Result!.OverrideReason ?? "Test timed out"),
            TestState.Cancelled => new CancelledTestNodeStateProperty(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private SessionUid CreateSessionUid(ExecutableTest test)
    {
        return _sessionUid ?? new SessionUid(Guid.NewGuid().ToString());
    }

    private async Task InvokeTestWithTimeout(ExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        var discoveredTest = test.Context.InternalDiscoveredTest;
        var testAction = test.Metadata.TimeoutMs.HasValue
            ? CreateTimeoutTestAction(test, instance, cancellationToken)
            : CreateNormalTestAction(test, instance, cancellationToken);

        await InvokeWithTestExecutor(discoveredTest, test.Context, testAction);
    }

    private Func<ValueTask> CreateTimeoutTestAction(ExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        return async () =>
        {
            // Inject properties into arguments just before test execution
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.ClassArguments);
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.Arguments);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(test.Metadata.TimeoutMs!.Value);

            var testTask = test.InvokeTestAsync(instance, cts.Token);
            var timeoutTask = Task.Delay(test.Metadata.TimeoutMs.Value, cancellationToken);
            var completedTask = await Task.WhenAny(testTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                cts.Cancel();
                await AttemptToCompleteTestTask(testTask);
                throw new OperationCanceledException($"Test '{test.DisplayName}' exceeded timeout of {test.Metadata.TimeoutMs.Value}ms");
            }

            await testTask;
        };
    }

    private Func<ValueTask> CreateNormalTestAction(ExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        return async () =>
        {
            // Inject properties into arguments just before test execution
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.ClassArguments);
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.Arguments);

            await test.InvokeTestAsync(instance, cancellationToken);
        };
    }

    private async Task AttemptToCompleteTestTask(Task testTask)
    {
        try
        {
            await testTask;
        }
        catch
        {
        }
    }

    private async Task InvokeWithTestExecutor(DiscoveredTest? discoveredTest, TestContext context, Func<ValueTask> testAction)
    {
        if (discoveredTest?.TestExecutor != null)
        {
            await discoveredTest.TestExecutor.ExecuteTest(context, testAction);
        }
        else
        {
            await testAction();
        }
    }


    private async Task DecrementAndDisposeTrackedObjectsAsync(ExecutableTest test)
    {
        var objectsToCheck = new List<object?>();
        objectsToCheck.AddRange(test.ClassArguments);
        objectsToCheck.AddRange(test.Arguments);

        if (test.Context?.TestDetails.TestClassInjectedPropertyArguments != null)
        {
            objectsToCheck.AddRange(test.Context.TestDetails.TestClassInjectedPropertyArguments.Values);
        }

        foreach (var obj in objectsToCheck)
        {
            if (obj == null)
            {
                continue;
            }

            if (ActiveObjectTracker.TryGetCounter(obj, out var counter))
            {
                var count = counter!.Decrement();
                if (count == 0)
                {
                    ActiveObjectTracker.RemoveObject(obj);

                    if (obj is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }

    private static void RestoreHookContexts(TestContext context)
    {
        if (context.ClassContext != null)
        {
            // Restore assembly context first
            var assemblyContext = context.ClassContext.AssemblyContext;
            AssemblyHookContext.Current = assemblyContext;

            // Then restore class context
            ClassHookContext.Current = context.ClassContext;
        }
    }
}
