using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Test executor that properly handles ExecutionContext restoration for AsyncLocal support
/// </summary>
public class SingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestResultFactory _resultFactory;
    private SessionUid? _sessionUid;

    public SingleTestExecutor(TUnitFrameworkLogger logger)
    {
        _logger = logger;
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
        if (test.State == TestState.Failed && test.Result != null)
        {
            return CreateUpdateMessage(test);
        }

        test.StartTime = DateTimeOffset.Now;
        test.State = TestState.Running;

        try
        {
            if (test.Metadata.IsSkipped)
            {
                return HandleSkippedTest(test);
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
        }

        return CreateUpdateMessage(test);
    }

    private TestNodeUpdateMessage HandleSkippedTest(ExecutableTest test)
    {
        test.State = TestState.Skipped;
        test.Result = _resultFactory.CreateSkippedResult(
            test.StartTime!.Value,
            test.Metadata.SkipReason ?? "Test skipped");
        test.EndTime = DateTimeOffset.Now;
        return CreateUpdateMessage(test);
    }

    private async Task ExecuteTestWithHooksAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        // Create test instance
        var instance = await test.CreateInstanceAsync();

        // Inject property values
        await InjectPropertyValuesAsync(instance, test.PropertyValues);

        // Set the instance in the test context for hooks
        test.Context!.TestDetails.ClassInstance = instance;

        // Restore ExecutionContext for AsyncLocal support
        test.Context!.RestoreExecutionContext();

        try
        {
            // Execute before test hooks
            await ExecuteBeforeTestHooksAsync(test.BeforeTestHooks, test.Context!, cancellationToken);

            // Execute the test
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
            // Execute after test hooks (always run)
            await ExecuteAfterTestHooksAsync(test.AfterTestHooks, test.Context!, cancellationToken);
        }
    }

    private async Task InjectPropertyValuesAsync(object instance, IDictionary<string, object?> propertyValues)
    {
        // Property injection is now handled via hooks with dependency injection
        // This method is kept for backward compatibility but does nothing
        await Task.CompletedTask;
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
                // Don't throw for after hooks - we want to run all of them
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
        if (test.Metadata.TimeoutMs.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(test.Metadata.TimeoutMs.Value);

            var testTask = test.InvokeTestAsync(instance, cts.Token);
            var timeoutTask = Task.Delay(test.Metadata.TimeoutMs.Value, cancellationToken);
            var completedTask = await Task.WhenAny(testTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Cancel the test task
                cts.Cancel();

                // Wait for the test task to complete (with cancellation) or throw
                try
                {
                    await testTask;
                }
                catch
                {
                    // Ignore exceptions from cancelled task
                }

                throw new OperationCanceledException($"Test '{test.DisplayName}' exceeded timeout of {test.Metadata.TimeoutMs.Value}ms");
            }

            // Test completed within timeout
            await testTask;
        }
        else
        {
            await test.InvokeTestAsync(instance, cancellationToken);
        }
    }
}
