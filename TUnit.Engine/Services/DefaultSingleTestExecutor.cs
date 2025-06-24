using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Default implementation of single test executor
/// </summary>
public class DefaultSingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    private SessionUid? _sessionUid;
    
    public DefaultSingleTestExecutor(TUnitFrameworkLogger logger)
    {
        _logger = logger;
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
            // Skip if requested
            if (test.Metadata.IsSkipped)
            {
                test.State = TestState.Skipped;
                test.Result = new TestResult
                {
                    Status = Core.Enums.Status.Skipped,
                    Start = test.StartTime,
                    End = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                    Exception = null,
                    ComputerName = Environment.MachineName,
                    OverrideReason = test.Metadata.SkipReason ?? "Test skipped"
                };
                test.EndTime = DateTimeOffset.Now;
                return CreateUpdateMessage(test);
            }
            
            // Create test instance
            var instance = await test.CreateInstance();
            
            // Inject property values
            foreach (var propertyValue in test.PropertyValues)
            {
                #pragma warning disable IL2075 // Test instance types are known at compile time
                var property = instance.GetType().GetProperty(propertyValue.Key);
                #pragma warning restore IL2075
                property?.SetValue(instance, propertyValue.Value);
            }
            
            // Execute hooks and test
            var hookContext = new HookContext(test.Context!, test.Metadata.TestClassType, instance);
            
            // Before class hooks
            foreach (var hook in test.Hooks.BeforeClass)
            {
                await hook(hookContext);
            }
            
            // After class hooks (instance created)
            foreach (var hook in test.Hooks.AfterClass)
            {
                await hook(instance, hookContext);
            }
            
            // Before test hooks
            foreach (var hook in test.Hooks.BeforeTest)
            {
                await hook(instance, hookContext);
            }
            
            try
            {
                // Execute test with timeout if specified
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
                
                test.State = TestState.Passed;
                test.Result = new TestResult 
                { 
                    Status = Core.Enums.Status.Passed,
                    Start = test.StartTime,
                    End = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                    Exception = null,
                    ComputerName = Environment.MachineName
                };
            }
            catch (OperationCanceledException) when (test.Metadata.TimeoutMs.HasValue)
            {
                test.State = TestState.Timeout;
                test.Result = new TestResult
                {
                    Status = Core.Enums.Status.Failed,
                    Start = test.StartTime,
                    End = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                    Exception = new TimeoutException($"Test exceeded timeout of {test.Metadata.TimeoutMs}ms"),
                    ComputerName = Environment.MachineName,
                    OverrideReason = $"Test exceeded timeout of {test.Metadata.TimeoutMs}ms"
                };
            }
            catch (Exception ex)
            {
                test.State = TestState.Failed;
                test.Result = new TestResult
                {
                    Status = Core.Enums.Status.Failed,
                    Start = test.StartTime,
                    End = DateTimeOffset.Now,
                    Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                    Exception = ex,
                    ComputerName = Environment.MachineName
                };
            }
            finally
            {
                // After test hooks
                try
                {
                    foreach (var hook in test.Hooks.AfterTest)
                    {
                        await hook(instance, hookContext);
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in after test hook: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                Status = Core.Enums.Status.Failed,
                Start = test.StartTime,
                End = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                Exception = ex,
                ComputerName = Environment.MachineName
            };
        }
        finally
        {
            test.EndTime = DateTimeOffset.Now;
        }
        
        return CreateUpdateMessage(test);
    }
    
    private TestNodeUpdateMessage CreateUpdateMessage(ExecutableTest test)
    {
        var properties = new PropertyBag();
        
        // Add test result properties
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
        
        var sessionUid = _sessionUid ?? 
            (test.Context?.Request?.Session?.SessionUid != null 
                ? new Microsoft.Testing.Platform.TestHost.SessionUid(test.Context.Request.Session.SessionUid.ToString())
                : new Microsoft.Testing.Platform.TestHost.SessionUid(Guid.NewGuid().ToString()));
            
        return new TestNodeUpdateMessage(
            sessionUid: sessionUid,
            testNode: new TestNode
            {
                Uid = new TestNodeUid(test.TestId),
                DisplayName = test.DisplayName,
                Properties = properties
            });
    }
}