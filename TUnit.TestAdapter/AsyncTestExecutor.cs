using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TUnit.Core;
using TUnit.Core.Attributes;

namespace TUnit.TestAdapter;

public class AsyncTestExecutor(CancellationTokenSource cancellationTokenSource)
{
    private bool _canRunAnotherTest = true;

    private readonly ConcurrentDictionary<string, Task> _oneTimeSetUpRegistry = new();
    private readonly ConcurrentDictionary<string, Task> _oneTimeTearDownRegistry = new();

    public async Task RunInAsyncContext(IEnumerable<TestWithTestCase> tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var allTestsOrderedByClass = tests
            .GroupBy(x => x.Test.FullyQualifiedClassName)
            .SelectMany(x => x.ToList())
            .ToList();
        
        var queue = new Queue<TestWithTestCase>(allTestsOrderedByClass);
        
        if (queue.Count is 0)
        {
            return;
        }
        
        MonitorSystemResources();

        var executingTests = new List<ProcessingTest>();
        
        await foreach (var testCase in ProcessQueue(queue, frameworkHandle))
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            executingTests.Add(testCase);

            SetupRunOneTimeTearDownForClass(testCase, allTestsOrderedByClass, executingTests);
            
            executingTests.RemoveAll(x => x.Task.IsCompletedSuccessfully);
        }
        
        executingTests.RemoveAll(x => x.Task.IsCompletedSuccessfully);

        await Task.WhenAll(executingTests.Select(x => x.Task));
        await Task.WhenAll(_oneTimeTearDownRegistry.Values);
    }

    private void SetupRunOneTimeTearDownForClass(ProcessingTest processingTest,
        IEnumerable<TestWithTestCase> allTestsOrderedByClass,
        IEnumerable<ProcessingTest> executingTests)
    {
        var lastTestForClass = allTestsOrderedByClass.Last(x =>
            x.Test.FullyQualifiedClassName == processingTest.Test.FullyQualifiedClassName);

        if (processingTest.Test.FullyQualifiedName != lastTestForClass.Test.FullyQualifiedName)
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

    private async IAsyncEnumerable<ProcessingTest> ProcessQueue(Queue<TestWithTestCase> queue, ITestExecutionRecorder? frameworkHandle)
    {
        while (queue.Count > 0)
        {
            if (_canRunAnotherTest && !cancellationTokenSource.IsCancellationRequested)
            {
                var test = queue.Dequeue();
                
                var @class = CreateTestClass(test.Test);
                
                yield return new ProcessingTest(test.Test, @class, ProcessTest(test, @class, frameworkHandle));
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

    private async Task ProcessTest(TestWithTestCase testWithTestCase, object @class, ITestExecutionRecorder? frameworkHandle)
    {
        await ExecuteTestMethod(testWithTestCase, @class, frameworkHandle);
    }

    private static object CreateTestClass(Test test)
    {
        return Activator.CreateInstance(test.MethodInfo.DeclaringType!)!;
    }

    private async ValueTask ExecuteTestMethod(TestWithTestCase testWithTestCase, object @class, ITestExecutionRecorder? frameworkHandle)
    {
        var test = testWithTestCase.Test;
        var testCase = testWithTestCase.TestCase;
        
        if (test.IsSkipped)
        {
            var skipTime = DateTimeOffset.Now;
            frameworkHandle?.RecordEnd(testCase, TestOutcome.Skipped);
            frameworkHandle?.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Skipped,
                DisplayName = test.TestName,
                StartTime = skipTime,
                EndTime = skipTime,
                Duration = TimeSpan.Zero,
                ComputerName = Environment.MachineName,
            });
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
        
        await InvokeMethod(@class, test.MethodInfo, BindingFlags.Default, test.ArgumentValues?.ToArray());
        
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
            await InvokeMethod(@class, setUpMethod, BindingFlags.Default, null);
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
                await InvokeMethod(@class, tearDownMethod, BindingFlags.Default, null);
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
            await InvokeMethod(@class, oneTimeSetUpMethod, BindingFlags.Static | BindingFlags.Public, null);
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

    private async Task InvokeMethod(object @class, MethodInfo methodInfo, BindingFlags bindingFlags, object?[]? arguments)
    {
        try
        {
            var result = await Task.Run<Task?>(() => methodInfo.Invoke(@class, bindingFlags, null, arguments, CultureInfo.InvariantCulture) as Task);

            if (result != null)
            {
                await result;
            }
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is null)
            {
                throw;
            }
            
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
        }
    }

    private bool IsTask(Type type)
    {
        return type.IsAssignableTo(typeof(Task));
    }
}