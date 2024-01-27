using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Core.Attributes;
using TUnit.Engine;

namespace TUnit.TestAdapter;

public class AsyncTestRunExecutor
    (
        SingleTestExecutor singleTestExecutor, 
        MethodInvoker methodInvoker, 
        ITestExecutionRecorder testExecutionRecorder, 
        CancellationTokenSource cancellationTokenSource
        )
{
    private bool _canRunAnotherTest = true;

    private readonly ConcurrentDictionary<string, Task> _oneTimeTearDownRegistry = new();
    private readonly List<Task> _setResultsTasks = new();

    public async Task RunInAsyncContext(IEnumerable<TestWithTestCase> tests)
    {
        var allTestsOrderedByClass = tests
            .GroupBy(x => x.Details.FullyQualifiedClassName)
            .SelectMany(x => x.ToList())
            .ToList();
        
        var queue = new Queue<TestWithTestCase>(allTestsOrderedByClass);
        
        if (queue.Count is 0)
        {
            return;
        }
        
        MonitorSystemResources();

        var executingTests = new List<TestWithResult>();
        
        await foreach (var testWithResult in ProcessQueue(queue))
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            executingTests.Add(testWithResult);

            SetupRunOneTimeTearDownForClass(testWithResult.Test.Details, allTestsOrderedByClass, executingTests);
            
            executingTests.RemoveAll(x => x.Result.IsCompletedSuccessfully);
            
            _setResultsTasks.Add(testWithResult.Result.ContinueWith(t =>
            {
                var result = t.Result;
                var testDetails = testWithResult.Test.Details;
                
                testExecutionRecorder?.RecordResult(new TestResult(testWithResult.Test.TestCase)
                {
                    DisplayName = testDetails.DisplayName,
                    Outcome = GetOutcome(result.Status),
                    ComputerName = result.ComputerName,
                    Duration = result.Duration,
                    StartTime = result.Start,
                    EndTime = result.End,
                    ErrorMessage = result.Exception?.Message,
                    ErrorStackTrace = result.Exception?.StackTrace,
                });
            }));
        }
        
        executingTests.RemoveAll(x => x.Result.IsCompletedSuccessfully);

        await WhenAllSafely(executingTests.Select(x => x.Result), testExecutionRecorder);
        await WhenAllSafely(_oneTimeTearDownRegistry.Values, testExecutionRecorder);
        await Task.WhenAll(_setResultsTasks);
    }

    private TestOutcome GetOutcome(Status resultStatus)
    {
        return resultStatus switch
        {
            Status.None => TestOutcome.None,
            Status.Passed => TestOutcome.Passed,
            Status.Failed => TestOutcome.Failed,
            Status.Skipped => TestOutcome.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(resultStatus), resultStatus, null)
        };
    }

    private void SetupRunOneTimeTearDownForClass(TestDetails processingTestDetails,
        IEnumerable<TestWithTestCase> allTestsOrderedByClass,
        IEnumerable<TestWithResult> executingTests)
    {
        var lastTestForClass = allTestsOrderedByClass.Last(x =>
            x.Details.FullyQualifiedClassName == processingTestDetails.FullyQualifiedClassName);

        if (processingTestDetails.FullyQualifiedName != lastTestForClass.Details.FullyQualifiedName)
        {
            return;
        }

        var executingTestsForThisClass = executingTests
            .Where(x => x.Test.Details.FullyQualifiedClassName == processingTestDetails.FullyQualifiedClassName)
            .Select(x => x.Result)
            .ToArray();

        Task.WhenAll(executingTestsForThisClass).ContinueWith(x =>
        {
            _ = _oneTimeTearDownRegistry.GetOrAdd(processingTestDetails.FullyQualifiedClassName,
                ExecuteOneTimeTearDowns(processingTestDetails));
            
            return Task.CompletedTask;
        });
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestWithTestCase> queue)
    {
        while (queue.Count > 0)
        {
            if (_canRunAnotherTest && !cancellationTokenSource.IsCancellationRequested)
            {
                var test = queue.Dequeue();

                yield return new TestWithResult(test, singleTestExecutor.ExecuteTest(test.Details));
            }
            else if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            else
            {
                await Task.Delay(100);
            }
        }
    }

    private void MonitorSystemResources()
    {
        Task.Factory.StartNew(async _ =>
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(500);
                
                var cpuUsage = await GetCpuUsageForProcess();

                _canRunAnotherTest = cpuUsage < 80;
            }
        }, null, TaskCreationOptions.LongRunning);
    }
    
    private async Task<double> GetCpuUsageForProcess()
    {
        var startTime = DateTime.UtcNow;
        
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        await Task.Delay(500);
    
        var endTime = DateTime.UtcNow;
        
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return cpuUsageTotal * 100;
    }
    
    private async Task ExecuteOneTimeTearDowns(TestDetails testDetails)
    {
        var oneTimeTearDownMethods = testDetails.MethodInfo.DeclaringType!
            .GetMethods()
            .Where(x => x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(OneTimeTearDownAttribute)));

        foreach (var oneTimeTearDownMethod in oneTimeTearDownMethods)
        {
            await methodInvoker.InvokeMethod(null, oneTimeTearDownMethod, BindingFlags.Static | BindingFlags.Public, null);
        }
    }

    private async Task WhenAllSafely(IEnumerable<Task> tasks, IMessageLogger? messageLogger)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            messageLogger?.SendMessage(TestMessageLevel.Error, e.ToString());
        }
    }
}