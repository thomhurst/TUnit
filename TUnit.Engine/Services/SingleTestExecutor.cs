using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Logging;
using TUnit.Core.ReferenceTracking;
using TUnit.Core.Tracking;
using TUnit.Engine.Exceptions;
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
    private readonly IHookCollectionService _hookCollectionService;
    private SessionUid? _sessionUid;

    public SingleTestExecutor(TUnitFrameworkLogger logger, EventReceiverOrchestrator eventReceiverOrchestrator, IHookCollectionService hookCollectionService)
    {
        _logger = logger;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _hookCollectionService = hookCollectionService;
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
        // If test is already failed (e.g., from data source expansion error),
        // just report the existing failure
        if (test is { State: TestState.Failed, Result: not null })
        {
            return CreateUpdateMessage(test);
        }

        TestContext.Current = test.Context;

        test.StartTime = DateTimeOffset.Now;
        test.State = TestState.Running;

        // Check if test is already marked as skipped (from basic SkipAttribute during discovery)
        if (!string.IsNullOrEmpty(test.Context.SkipReason))
        {
            return await HandleSkippedTestAsync(test, cancellationToken);
        }

        // Check if we already have a skipped test instance from discovery
        if (test.Context.TestDetails.ClassInstance is SkippedTestInstance)
        {
            return await HandleSkippedTestAsync(test, cancellationToken);
        }

        var instance = await test.CreateInstanceAsync();
        test.Context.TestDetails.ClassInstance = instance;

        await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.ClassArguments, test.Context.ObjectBag, test.Context.TestDetails.MethodMetadata, test.Context.Events);
        await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.Arguments, test.Context.ObjectBag, test.Context.TestDetails.MethodMetadata, test.Context.Events);

        await PropertyInjector.InjectPropertiesAsync(
            test.Context,
            instance,
            test.Metadata.PropertyDataSources,
            test.Metadata.PropertyInjections,
            test.Metadata.MethodMetadata,
            test.Context.TestDetails.TestId);

        await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken);

        var classContext = test.Context.ClassContext;
        var assemblyContext = classContext.AssemblyContext;
        var sessionContext = assemblyContext.TestSessionContext;

        await _eventReceiverOrchestrator.InvokeFirstTestInSessionEventReceiversAsync(test.Context, sessionContext, cancellationToken);

        await _eventReceiverOrchestrator.InvokeFirstTestInAssemblyEventReceiversAsync(test.Context, assemblyContext, cancellationToken);

        await _eventReceiverOrchestrator.InvokeFirstTestInClassEventReceiversAsync(test.Context, classContext, cancellationToken);
        await _eventReceiverOrchestrator.InvokeTestStartEventReceiversAsync(test.Context, cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(test.Context.SkipReason))
            {
                return await HandleSkippedTestAsync(test, cancellationToken);
            }

            if(test.Context is { RetryFunc: not null, TestDetails.RetryLimit: > 0 })
            {

                await ExecuteTestWithRetries(() => ExecuteTestWithHooksAsync(test, instance, cancellationToken), test.Context, cancellationToken);
            }
            else
            {
                await ExecuteTestWithHooksAsync(test, instance, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            HandleTestFailure(test, ex);
        }
        finally
        {
            test.EndTime = DateTimeOffset.Now;

            await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context!, cancellationToken);

            var endClassContext = test.Context!.ClassContext;
            var endAssemblyContext = endClassContext.AssemblyContext;
            var endSessionContext = endAssemblyContext.TestSessionContext;

            await _eventReceiverOrchestrator.InvokeLastTestInClassEventReceiversAsync(test.Context, endClassContext, cancellationToken);

            await _eventReceiverOrchestrator.InvokeLastTestInAssemblyEventReceiversAsync(test.Context, endAssemblyContext, cancellationToken);
            await _eventReceiverOrchestrator.InvokeLastTestInSessionEventReceiversAsync(test.Context, endSessionContext, cancellationToken);
        }

        return CreateUpdateMessage(test);
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
                await testDelegate();
                return;
            }
            catch (Exception ex) when (i < retryLimit)
            {
                if (!await retryFunc(testContext, ex, i + 1))
                {
                    throw;
                }

                await _logger.LogWarningAsync($"Retrying test due to exception: {ex.Message}. Attempt {i} of {retryLimit}.");
            }
        }
    }

    private async Task<TestNodeUpdateMessage> HandleSkippedTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Skipped;

        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Context.SkipReason ?? "Test skipped");

        test.EndTime = DateTimeOffset.Now;
        await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken);

        return CreateUpdateMessage(test);
    }

    private async Task ExecuteTestWithHooksAsync(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        RestoreHookContexts(test.Context);

        // Collect hooks lazily at execution time
        var testClassType = test.Context.TestDetails.ClassType;
        var beforeTestHooks = await _hookCollectionService.CollectBeforeTestHooksAsync(testClassType);
        var afterTestHooks = await _hookCollectionService.CollectAfterTestHooksAsync(testClassType);

        Exception? testException = null;
        try
        {
            await ExecuteBeforeTestHooksAsync(beforeTestHooks, test.Context, cancellationToken);

            test.Context.RestoreExecutionContext();

            await InvokeTestWithTimeout(test, instance, cancellationToken);

            test.State = TestState.Passed;
            test.Result = _resultFactory.CreatePassedResult(test.StartTime!.Value);
        }
        catch (Exception ex)
        {
            HandleTestFailure(test, ex);
            testException = ex;
        }

        try
        {
            await ExecuteAfterTestHooksAsync(afterTestHooks, test.Context, cancellationToken);
        }
        catch (Exception afterHookEx)
        {
            // If test already failed, aggregate the exceptions
            if (testException != null)
            {
                throw new AggregateException("Test and after hook both failed", testException, afterHookEx);
            }

            // Otherwise, fail the test due to after hook failure
            HandleTestFailure(test, afterHookEx);
            throw;
        }
        finally
        {
            await DecrementAndDisposeTrackedObjectsAsync(test);

            // Dispose the test class instance if it implements IDisposable or IAsyncDisposable
            if (instance is IAsyncDisposable asyncDisposableInstance)
            {
                await asyncDisposableInstance.DisposeAsync();
            }
            else if (instance is IDisposable disposableInstance)
            {
                disposableInstance.Dispose();
            }
        }

        // Re-throw original test exception if after hooks succeeded
        if (testException != null)
        {
            throw testException;
        }
    }


    private async Task ExecuteBeforeTestHooksAsync(IReadOnlyList<Func<TestContext, CancellationToken, Task>> hooks, TestContext context, CancellationToken cancellationToken)
    {
        RestoreHookContexts(context);
        context.RestoreExecutionContext();

        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);

                context.RestoreExecutionContext();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in before test hook: {ex.Message}");
                throw;
            }
        }
    }

    private async Task ExecuteAfterTestHooksAsync(IReadOnlyList<Func<TestContext, CancellationToken, Task>> hooks, TestContext context, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();

        foreach (var hook in hooks)
        {
            try
            {
                RestoreHookContexts(context);

                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in after test hook: {ex.Message}");
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            if (exceptions.Count == 1)
            {
                throw new HookFailedException(exceptions[0]);
            }
            else
            {
                throw new HookFailedException("Multiple after test hooks failed", new AggregateException(exceptions));
            }
        }
    }

    private void HandleTestFailure(AbstractExecutableTest test, Exception ex)
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

        var sessionUid = _sessionUid ?? CreateSessionUid(test);

        return new TestNodeUpdateMessage(
            sessionUid: sessionUid,
            testNode: testNode);
    }

    private IProperty GetTestNodeState(AbstractExecutableTest test)
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

    private SessionUid CreateSessionUid(AbstractExecutableTest test)
    {
        return _sessionUid ?? new SessionUid(Guid.NewGuid().ToString());
    }

    private async Task InvokeTestWithTimeout(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        var discoveredTest = test.Context.InternalDiscoveredTest;
        var testAction = test.Metadata.TimeoutMs.HasValue
            ? CreateTimeoutTestAction(test, instance, cancellationToken)
            : CreateNormalTestAction(test, instance, cancellationToken);

        await InvokeWithTestExecutor(discoveredTest, test.Context, testAction);
    }

    private Func<ValueTask> CreateTimeoutTestAction(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(test.Metadata.TimeoutMs!.Value);

            var testTask = test.InvokeTestAsync(instance, cts.Token);
            var timeoutTask = Task.Delay(test.Metadata.TimeoutMs.Value, cancellationToken);
            var completedTask = await Task.WhenAny(testTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                cts.Cancel();
                throw new OperationCanceledException($"Test '{test.Context.GetDisplayName()}' exceeded timeout of {test.Metadata.TimeoutMs.Value}ms");
            }

            await testTask;
        };
    }

    private Func<ValueTask> CreateNormalTestAction(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        return async () =>
        {
            await test.InvokeTestAsync(instance, cancellationToken);
        };
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


    private async Task DecrementAndDisposeTrackedObjectsAsync(AbstractExecutableTest test)
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

            if (ObjectTracker.TryGetReference(obj, out var counter))
            {
                var count = counter!.Decrement();
                if (count == 0)
                {
                    ObjectTracker.RemoveObject(obj);

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
            var assemblyContext = context.ClassContext.AssemblyContext;
            AssemblyHookContext.Current = assemblyContext;

            ClassHookContext.Current = context.ClassContext;
        }
    }
}
