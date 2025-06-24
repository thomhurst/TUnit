using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Test executor that properly handles ExecutionContext restoration for AsyncLocal support
/// </summary>
public class ExecutionContextAwareSingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestResultFactory _resultFactory;
    private SessionUid? _sessionUid;
    
    public ExecutionContextAwareSingleTestExecutor(TUnitFrameworkLogger logger)
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
        var instance = await test.CreateInstance();
        
        // Inject property values
        await InjectPropertyValuesAsync(instance, test.PropertyValues);
        
        // Create hook context and restore ExecutionContext
        var hookContext = new HookContext(test.Context!, test.Metadata.TestClassType, instance);
        test.Context!.RestoreExecutionContext();
        
        try
        {
            // Execute lifecycle hooks
            await ExecuteHooksAsync(test.Hooks.BeforeClass, hookContext, null);
            await ExecuteHooksAsync(test.Hooks.AfterClass, hookContext, instance);
            await ExecuteHooksAsync(test.Hooks.BeforeTest, hookContext, instance);
            
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
            await ExecuteAfterTestHooksAsync(test.Hooks.AfterTest, hookContext, instance);
        }
    }
    
    private async Task InjectPropertyValuesAsync(object instance, IDictionary<string, object?> propertyValues)
    {
        foreach (var propertyValue in propertyValues)
        {
            #pragma warning disable IL2075 // Test instance types are known at compile time
            var property = instance.GetType().GetProperty(propertyValue.Key);
            #pragma warning restore IL2075
            property?.SetValue(instance, propertyValue.Value);
        }
        await Task.CompletedTask;
    }
    
    private async Task ExecuteHooksAsync(Func<HookContext, Task>[] hooks, HookContext context, object? instance)
    {
        foreach (var hook in hooks)
        {
            await hook(context);
        }
    }
    
    private async Task ExecuteHooksAsync(Func<object, HookContext, Task>[] hooks, HookContext context, object instance)
    {
        foreach (var hook in hooks)
        {
            await hook(instance, context);
        }
    }
    
    private async Task ExecuteAfterTestHooksAsync(Func<object, HookContext, Task>[] hooks, HookContext context, object instance)
    {
        try
        {
            await ExecuteHooksAsync(hooks, context, instance);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error in after test hook: {ex.Message}");
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
        var properties = CreateTestResultProperties(test);
        
        var sessionUid = _sessionUid ?? CreateSessionUid(test);
            
        return new TestNodeUpdateMessage(
            sessionUid: sessionUid,
            testNode: new TestNode
            {
                Uid = new TestNodeUid(test.TestId),
                DisplayName = test.DisplayName,
                Properties = properties
            });
    }
    
    private PropertyBag CreateTestResultProperties(ExecutableTest test)
    {
        var properties = new PropertyBag();
        
        // Add state-specific properties
        switch (test.State)
        {
            case TestState.Passed:
                properties.Add(new PassedTestNodeStateProperty());
                break;
            case TestState.Failed:
                properties.Add(new FailedTestNodeStateProperty(test.Result!.Exception!));
                break;
            case TestState.Skipped:
                properties.Add(new SkippedTestNodeStateProperty(test.Result!.OverrideReason ?? "Test skipped"));
                break;
            case TestState.Timeout:
                properties.Add(new TimeoutTestNodeStateProperty(test.Result!.OverrideReason ?? "Test timed out"));
                break;
            case TestState.Cancelled:
                properties.Add(new CancelledTestNodeStateProperty());
                break;
        }
        
        // Add timing info
        if (test.Duration.HasValue)
        {
            properties.Add(new TimingProperty(test.Duration.Value));
        }
        
        return properties;
    }
    
    private SessionUid CreateSessionUid(ExecutableTest test)
    {
        if (test.Context?.Request?.Session?.SessionUid != null)
        {
            return new SessionUid(test.Context.Request.Session.SessionUid.ToString());
        }
        return new SessionUid(Guid.NewGuid().ToString());
    }
    
    private async Task InvokeTestWithTimeout(ExecutableTest test, object instance, CancellationToken cancellationToken)
    {
        if (test.Metadata.TimeoutMs.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(test.Metadata.TimeoutMs.Value);
            
            var testTask = test.InvokeTest(instance);
            var completedTask = await Task.WhenAny(testTask, Task.Delay(test.Metadata.TimeoutMs.Value, cts.Token));
            
            if (completedTask != testTask)
            {
                throw new OperationCanceledException();
            }
            
            await testTask;
        }
        else
        {
            await test.InvokeTest(instance);
        }
    }
}

/// <summary>
/// Factory for creating test results
/// </summary>
internal interface ITestResultFactory
{
    TestResult CreatePassedResult(DateTimeOffset startTime);
    TestResult CreateFailedResult(DateTimeOffset startTime, Exception exception);
    TestResult CreateSkippedResult(DateTimeOffset startTime, string reason);
    TestResult CreateTimeoutResult(DateTimeOffset startTime, int timeoutMs);
}

/// <summary>
/// Default test result factory implementation
/// </summary>
internal sealed class TestResultFactory : ITestResultFactory
{
    public TestResult CreatePassedResult(DateTimeOffset startTime)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult 
        { 
            Status = Core.Enums.Status.Passed,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = null,
            ComputerName = Environment.MachineName
        };
    }
    
    public TestResult CreateFailedResult(DateTimeOffset startTime, Exception exception)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            Status = Core.Enums.Status.Failed,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = exception,
            ComputerName = Environment.MachineName
        };
    }
    
    public TestResult CreateSkippedResult(DateTimeOffset startTime, string reason)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            Status = Core.Enums.Status.Skipped,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = null,
            ComputerName = Environment.MachineName,
            OverrideReason = reason
        };
    }
    
    public TestResult CreateTimeoutResult(DateTimeOffset startTime, int timeoutMs)
    {
        var endTime = DateTimeOffset.Now;
        return new TestResult
        {
            Status = Core.Enums.Status.Failed,
            Start = startTime,
            End = endTime,
            Duration = endTime - startTime,
            Exception = new TimeoutException($"Test exceeded timeout of {timeoutMs}ms"),
            ComputerName = Environment.MachineName,
            OverrideReason = $"Test exceeded timeout of {timeoutMs}ms"
        };
    }
}