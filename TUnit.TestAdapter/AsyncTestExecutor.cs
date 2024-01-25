using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TUnit.Core;
using TUnit.Core.Attributes;
using TUnit.TestAdapter.Extensions;

namespace TUnit.TestAdapter;

public class AsyncTestExecutor(CancellationTokenSource cancellationTokenSource)
{
    private bool _canRunAnotherTest = true;

    private readonly ConcurrentDictionary<string, Task> _oneTimeSetUpRegistry = new();
    private readonly ConcurrentDictionary<string, Task> _oneTimeTearDownRegistry = new();

    public async Task RunInAsyncContext(IEnumerable<Test> tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var allTestsOrderedByClass = tests
            .GroupBy(x => x.FullyQualifiedClassName)
            .SelectMany(x => x.ToList())
            .ToList();
        
        var queue = new Queue<Test>(allTestsOrderedByClass);
        
        if (queue.Count is 0)
        {
            return;
        }
        
        MonitorSystemResources();

        var executingTests = new List<ProcessingTest>();
        
        await foreach (var testCase in ProcessQueue(queue, frameworkHandle))
        {
            executingTests.Add(testCase);

            SetupRunOneTimeTearDownForClass(testCase, allTestsOrderedByClass, executingTests);
            
            executingTests.RemoveAll(x => x.Task.IsCompletedSuccessfully);
        }
        
        executingTests.RemoveAll(x => x.Task.IsCompletedSuccessfully);

        await Task.WhenAll(executingTests.Select(x => x.Task));
        await Task.WhenAll(_oneTimeTearDownRegistry.Values);
    }

    private void SetupRunOneTimeTearDownForClass(ProcessingTest processingTest,
        IEnumerable<Test> allTestsOrderedByClass,
        IEnumerable<ProcessingTest> executingTests)
    {
        var lastTestForClass = allTestsOrderedByClass.Last(x =>
            x.FullyQualifiedClassName == processingTest.Test.FullyQualifiedClassName);

        if (processingTest.Test.FullName != lastTestForClass.FullName)
        {
            return;
        }

        var executingTestsForThisClass = executingTests
            .Where(x => x.Test.FullyQualifiedClassName == processingTest.Test.FullyQualifiedClassName)
            .Select(x => x.Task)
            .ToArray();

        Task.WhenAll(executingTestsForThisClass).ContinueWith(x =>
        {
            _ = _oneTimeTearDownRegistry.GetOrAdd(processingTest.Test.FullyQualifiedClassName,
                ExecuteOneTimeTearDowns(processingTest.Class));
            
            return Task.CompletedTask;
        });
    }

    private async IAsyncEnumerable<ProcessingTest> ProcessQueue(Queue<Test> queue, ITestExecutionRecorder? frameworkHandle)
    {
        while (queue.Count > 0)
        {
            if (_canRunAnotherTest)
            {
                var test = queue.Dequeue();
                
                var @class = CreateTestClass(test);
                
                yield return new ProcessingTest(test, @class, ProcessTest(test, @class, frameworkHandle));
            }
            else
            {
                await Task.Delay(100);
            }
        }
    }

    private async Task ProcessTest(Test test, object @class, ITestExecutionRecorder? frameworkHandle)
    {
        await ExecuteTestMethod(test, @class, frameworkHandle);
    }

    private static object CreateTestClass(Test test)
    {
        return Activator.CreateInstance(test.MethodInfo.DeclaringType!)!;
    }

    private async ValueTask ExecuteTestMethod(Test test, object @class, ITestExecutionRecorder? frameworkHandle)
    {
        var testCase = test.ToTestCase();

        if (test.IsSkipped)
        {
            frameworkHandle?.RecordEnd(testCase, TestOutcome.Skipped);
            return;
        }

        frameworkHandle?.RecordStart(testCase);

        var start = DateTimeOffset.Now;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await ExecuteTest(test, @class);

            frameworkHandle?.RecordEnd(testCase, TestOutcome.Passed);
            
            frameworkHandle?.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Passed,

                DisplayName = test.TestName,
                StartTime = start,
                EndTime = DateTimeOffset.Now,
                Duration = stopwatch.Elapsed,
                ComputerName = Environment.MachineName,
            });
        }
        catch (Exception e)
        {
            frameworkHandle?.RecordEnd(testCase, TestOutcome.Failed);
            
            frameworkHandle?.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Failed,

                DisplayName = test.TestName,
                ErrorMessage = e.Message,
                ErrorStackTrace = e.StackTrace,
                StartTime = start,
                EndTime = DateTimeOffset.Now,
                Duration = stopwatch.Elapsed,
                ComputerName = Environment.MachineName,
            });
        }
    }

    private async Task ExecuteTest(Test test, object @class)
    {
        await ExecuteSetUps(@class);
        
        var methodExecutionObject = test.MethodInfo.Invoke(@class, test.Arguments);

        if (methodExecutionObject is Task task)
        {
            await task;
        }
        
        await ExecuteTearDowns(@class);
    }

    private async Task ExecuteSetUps(object @class)
    {
        await _oneTimeSetUpRegistry.GetOrAdd(@class.GetType().FullName!, _ => ExecuteOneTimeSetUps(@class));

        var setUpMethods = @class.GetType()
            .GetMethods()
            .Where(x => !x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(SetUpAttribute)));

        foreach (var setUpMethod in setUpMethods)
        {
            var result = setUpMethod.Invoke(@class, null);

            if (result is Task task)
            {
                await task;
            }
        }
    }
    
    private async Task ExecuteTearDowns(object @class)
    {
        var tearDownMethods = @class.GetType()
            .GetMethods()
            .Where(x => !x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(TearDownAttribute)));

        var exceptions = new List<Exception>();
        
        foreach (var tearDownMethod in tearDownMethods)
        {
            try
            {
                var result = tearDownMethod.Invoke(@class, null);

                if (result is Task task)
                {
                    await task;
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }
        
        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }

    private async Task ExecuteOneTimeSetUps(object @class)
    {
        var oneTimeSetUpMethods = @class.GetType()
            .GetMethods()
            .Where(x => x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(OneTimeSetUpAttribute)));

        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            var result = oneTimeSetUpMethod.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, null, null);

            if (result is Task task)
            {
                await task;
            }
        }
    }
    
    private async Task ExecuteOneTimeTearDowns(object @class)
    {
        var oneTimeTearDownMethods = @class.GetType()
            .GetMethods()
            .Where(x => x.IsStatic)
            .Where(x => x.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(OneTimeTearDownAttribute)));

        foreach (var oneTimeTearDownMethod in oneTimeTearDownMethods)
        {
            var result = oneTimeTearDownMethod.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, null, null);

            if (result is Task task)
            {
                await task;
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
}