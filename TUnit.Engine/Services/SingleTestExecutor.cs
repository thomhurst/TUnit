using System.Runtime.ExceptionServices;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// Handles ExecutionContext restoration for AsyncLocal support and test lifecycle management
internal class SingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestResultFactory _resultFactory;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly IHookCollectionService _hookCollectionService;
    private readonly EngineCancellationToken _engineCancellationToken;
    private SessionUid _sessionUid;

    public SingleTestExecutor(TUnitFrameworkLogger logger,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        IHookCollectionService hookCollectionService,
        EngineCancellationToken engineCancellationToken,
        SessionUid sessionUid)
    {
        _logger = logger;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _hookCollectionService = hookCollectionService;
        _engineCancellationToken = engineCancellationToken;
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
        await ExecuteTestInternalAsync(test, cancellationToken);

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
                return await HandleSkippedTestInternalAsync(test, cancellationToken);
            }

            if (test.Context.TestDetails.ClassInstance is SkippedTestInstance)
            {
                return await HandleSkippedTestInternalAsync(test, cancellationToken);
            }

            if (test.Context.TestDetails.ClassInstance is PlaceholderInstance)
            {
                var createdInstance = await test.CreateInstanceAsync();
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
                test.Context.Events);
            await PropertyInjectionService.InjectPropertiesIntoArgumentsAsync(test.Arguments, test.Context.ObjectBag, test.Context.TestDetails.MethodMetadata,
                test.Context.Events);

            await PropertyInjectionService.InjectPropertiesAsync(
                test.Context,
                instance,
                test.Metadata.PropertyDataSources,
                test.Metadata.PropertyInjections,
                test.Metadata.MethodMetadata,
                test.Context.TestDetails.TestId);

            await _eventReceiverOrchestrator.InitializeAllEligibleObjectsAsync(test.Context, cancellationToken);

            // Populate TestContext.Dependencies from resolved dependencies
            PopulateTestContextDependencies(test);

            CheckDependenciesAndThrowIfShouldSkip(test);

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
                    return await HandleSkippedTestInternalAsync(test, cancellationToken);
                }

                if (test.Context is { RetryFunc: not null, TestDetails.RetryLimit: > 0 })
                {
                    await ExecuteTestWithRetries(() => ExecuteTestWithHooksAsync(test, instance, cancellationToken), test.Context, cancellationToken);
                }
                else
                {
                    await ExecuteTestWithHooksAsync(test, instance, cancellationToken);
                }
            }
            catch (TestDependencyException e)
            {
                test.Context.SkipReason = e.Message;
                return await HandleSkippedTestInternalAsync(test, cancellationToken);
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

                await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context!, cancellationToken);
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

    private async Task<TestResult> HandleSkippedTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Skipped;

        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Context.SkipReason ?? "Test skipped");

        test.EndTime = DateTimeOffset.Now;
        await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken);

        return test.Result;
    }


    private async Task ExecuteTestWithHooksAsync(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        // Context restoration is now handled inside ExecuteBeforeTestHooksAsync
        var testClassType = test.Context.TestDetails.ClassType;
        var beforeTestHooks = await _hookCollectionService.CollectBeforeTestHooksAsync(testClassType);
        var afterTestHooks = await _hookCollectionService.CollectAfterTestHooksAsync(testClassType);

        Exception? testException = null;
        try
        {
            await ExecuteBeforeTestHooksAsync(beforeTestHooks, test.Context, cancellationToken);

            // RestoreExecutionContext only if needed for the test itself
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
            if (testException != null)
            {
                throw new AggregateException("Test and after hook both failed", testException, afterHookEx);
            }

            HandleTestFailure(test, afterHookEx);
            throw;
        }
        finally
        {
            if (instance is IAsyncDisposable asyncDisposableInstance)
            {
                await asyncDisposableInstance.DisposeAsync();
            }
            else if (instance is IDisposable disposableInstance)
            {
                disposableInstance.Dispose();
            }
        }

        if (testException != null)
        {
            ExceptionDispatchInfo.Capture(testException).Throw();
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

                // RestoreExecutionContext after each hook to ensure AsyncLocal values flow correctly
                // when AddAsyncLocalValues() is called in hooks
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

        // Restore contexts once at the beginning
        RestoreHookContexts(context);

        foreach (var hook in hooks)
        {
            try
            {
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

    private async Task InvokeTestWithTimeout(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        var discoveredTest = test.Context.InternalDiscoveredTest;
        var testAction = test.Context.TestDetails.Timeout.HasValue
            ? CreateTimeoutTestAction(test, instance, cancellationToken)
            : CreateNormalTestAction(test, instance, cancellationToken);

        await InvokeWithTestExecutor(discoveredTest, test.Context, testAction);
    }

    private Func<ValueTask> CreateTimeoutTestAction(AbstractExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = (int)test.Context.TestDetails.Timeout!.Value.TotalMilliseconds;
            cts.CancelAfter(timeoutMs);

            try
            {
                await test.InvokeTestAsync(instance, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // This was our timeout, not an external cancellation
                throw new System.TimeoutException($"Test '{test.Context.GetDisplayName()}' exceeded timeout of {timeoutMs}ms");
            }
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



    private static void RestoreHookContexts(TestContext context)
    {
        if (context.ClassContext != null)
        {
            var assemblyContext = context.ClassContext.AssemblyContext;
            AssemblyHookContext.Current = assemblyContext;

            ClassHookContext.Current = context.ClassContext;
        }
    }

    private void PopulateTestContextDependencies(AbstractExecutableTest test)
    {
        // Clear any existing dependencies first
        test.Context.Dependencies.Clear();
        
        // Use a HashSet to avoid duplicates when collecting transitive dependencies
        var allDependencies = new HashSet<TestDetails>();
        
        // Recursively collect all transitive dependencies
        CollectTransitiveDependencies(test, allDependencies, new HashSet<AbstractExecutableTest>());
        
        // Add all collected dependencies to the TestContext
        test.Context.Dependencies.AddRange(allDependencies);
    }
    
    private void CollectTransitiveDependencies(AbstractExecutableTest test, HashSet<TestDetails> collected, HashSet<AbstractExecutableTest> visited)
    {
        // Avoid infinite recursion in case of circular dependencies
        if (!visited.Add(test))
        {
            return;
        }
        
        // Add all direct dependencies and their transitive dependencies
        foreach (var resolvedDependency in test.Dependencies)
        {
            var dependencyTest = resolvedDependency.Test;
            if (dependencyTest.Context?.TestDetails != null)
            {
                collected.Add(dependencyTest.Context.TestDetails);
                
                // Recursively collect transitive dependencies
                CollectTransitiveDependencies(dependencyTest, collected, visited);
            }
        }
    }

    private void CheckDependenciesAndThrowIfShouldSkip(AbstractExecutableTest test)
    {
        var failedDependenciesNotAllowingProceed = new List<string>();

        foreach (var dependency in test.Dependencies)
        {
            // Check if the dependency has failed or timed out
            if (dependency.Test.State == TestState.Failed || dependency.Test.State == TestState.Timeout)
            {
                // If this dependency doesn't allow proceeding on failure, add it to the list
                if (!dependency.ProceedOnFailure)
                {
                    var dependencyName = GetDependencyDisplayName(dependency.Test);
                    failedDependenciesNotAllowingProceed.Add(dependencyName);
                }
            }
        }

        // Only throw if there are dependencies that don't allow proceeding
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
}
