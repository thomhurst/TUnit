using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Test executor that properly handles ExecutionContext restoration for AsyncLocal support
/// </summary>
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
        
        // Initialize all eligible objects before test starts
        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context!, cancellationToken);
        
        // Invoke first test event receivers if this is the first test
        var classContext = test.Context!.ClassContext;
        if (classContext != null)
        {
            var assemblyContext = classContext.AssemblyContext;
            var sessionContext = assemblyContext.TestSessionContext;
            
            // First test in session
            await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(test.Context, sessionContext, cancellationToken);
            
            // First test in assembly
            await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(test.Context, assemblyContext, cancellationToken);
            
            // First test in class
            await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(test.Context, classContext, cancellationToken);
        }
        
        // Invoke test start event receivers
        await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(test.Context!, cancellationToken);

        try
        {
            if (test.Metadata.IsSkipped)
            {
                return await HandleSkippedTestAsync(test, cancellationToken);
            }

            await ExecuteTestWithHooksAsync(test, cancellationToken);
        }
        catch (Exception ex)
        {
            HandleTestFailure(test, ex);
        }
        finally
        {
            test.EndTime = DateTimeOffset.Now;
            
            // Invoke test end event receivers
            await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context!, cancellationToken);
            
            // Invoke last test event receivers if this is the last test
            var endClassContext = test.Context!.ClassContext;
            if (endClassContext != null)
            {
                var endAssemblyContext = endClassContext.AssemblyContext;
                var endSessionContext = endAssemblyContext.TestSessionContext;
                
                // Last test in class
                await _eventReceiverOrchestrator.InvokeLastTestInClassEventReceiversAsync(test.Context, endClassContext, cancellationToken);
                
                // Last test in assembly
                await _eventReceiverOrchestrator.InvokeLastTestInAssemblyEventReceiversAsync(test.Context, endAssemblyContext, cancellationToken);
                
                // Last test in session
                await _eventReceiverOrchestrator.InvokeLastTestInSessionEventReceiversAsync(test.Context, endSessionContext, cancellationToken);
            }
        }

        return CreateUpdateMessage(test);
    }

    private async Task<TestNodeUpdateMessage> HandleSkippedTestAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Skipped;
        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Metadata.SkipReason ?? "Test skipped");
        test.EndTime = DateTimeOffset.Now;
        
        // Invoke test skipped event receivers
        await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context!, cancellationToken);
        
        return CreateUpdateMessage(test);
    }

    private async Task ExecuteTestWithHooksAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        var instance = await test.CreateInstanceAsync();

        test.Context!.TestDetails.ClassInstance = instance;

        test.Context!.RestoreExecutionContext();

        try
        {
            await ExecuteBeforeTestHooksAsync(test.BeforeTestHooks, test.Context!, cancellationToken);

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
            await ExecuteAfterTestHooksAsync(test.AfterTestHooks, test.Context!, cancellationToken);
            await DecrementAndDisposeTrackedObjectsAsync(test);
        }
    }


    private async Task ExecuteBeforeTestHooksAsync(Func<TestContext, CancellationToken, Task>[] hooks, TestContext context, CancellationToken cancellationToken)
    {
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
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

    private TestNodeUpdateMessage CreateUpdateMessage(ExecutableTest test)
    {
        var testNode = test.Context!.ToTestNode()
            .WithProperty(GetTestNodeState(test));

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

        Func<ValueTask> testAction;
        if (test.Metadata.TimeoutMs.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(test.Metadata.TimeoutMs.Value);

            testAction = async () =>
            {
                var testTask = test.InvokeTestAsync(instance, cts.Token);
                var timeoutTask = Task.Delay(test.Metadata.TimeoutMs.Value, cancellationToken);
                var completedTask = await Task.WhenAny(testTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    cts.Cancel();

                    try
                    {
                        await testTask;
                    }
                    catch
                    {
                    }

                    throw new OperationCanceledException($"Test '{test.DisplayName}' exceeded timeout of {test.Metadata.TimeoutMs.Value}ms");
                }

                await testTask;
            };
        }
        else
        {
            testAction = async () => await test.InvokeTestAsync(instance, cancellationToken);
        }

        if (discoveredTest?.TestExecutor != null)
        {
            await discoveredTest.TestExecutor.ExecuteTest(test.Context, testAction);
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
}
