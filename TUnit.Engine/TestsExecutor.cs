using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine;

internal class TestsExecutor
{
    private int _currentlyExecutingTests;

    private readonly SingleTestExecutor _singleTestExecutor;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TestGrouper _testGrouper;
    private readonly SystemResourceMonitor _systemResourceMonitor;
    private readonly ConsoleInterceptor _consoleInterceptor;
    private readonly ILogger<TestsExecutor> _logger;

    public TestsExecutor(SingleTestExecutor singleTestExecutor,
        CancellationTokenSource cancellationTokenSource,
        TestGrouper testGrouper,
        SystemResourceMonitor systemResourceMonitor,
        ConsoleInterceptor consoleInterceptor,
        ILoggerFactory loggerFactory)
    {
        _singleTestExecutor = singleTestExecutor;
        _cancellationTokenSource = cancellationTokenSource;
        _testGrouper = testGrouper;
        _systemResourceMonitor = systemResourceMonitor;
        _consoleInterceptor = consoleInterceptor;
        _logger = loggerFactory.CreateLogger<TestsExecutor>();
    }

    public async Task ExecuteAsync(IEnumerable<TestInformation> testNodes, TestSessionContext session)
    {
        _consoleInterceptor.Initialize();
        
        var start = DateTimeOffset.Now;

        try
        {
            await AssemblyHookOrchestrators.ExecuteSetups();
        
            var tests = _testGrouper.OrganiseTests(testNodes);
            
            foreach (var test in tests.AllTests)
            {
                var matchingTest = TestDictionary.GetTest(test.TestId);
                ClassHookOrchestrator.RegisterInstance(matchingTest.TestContext.TestInformation.ClassType);
            }
        
            // TODO: I don't love this - Late setting a property.
            _singleTestExecutor.SetAllTests(tests);
        
            await ProcessTests(tests.Parallel, true, session);

            await ProcessKeyedNotInParallelTests(tests.KeyedNotInParallel, session);
        
            await ProcessTests(tests.NotInParallel, false, session);
        }
        finally
        {
            await AssemblyHookOrchestrators.ExecuteCleanups();
            await _logger.LogInformationAsync(
                $"Tests finished after {(start - DateTimeOffset.Now).TotalSeconds} seconds");
        }
    }

    private async Task ProcessKeyedNotInParallelTests(List<NotInParallelTestCase> testsToProcess, TestSessionContext session)
    {
        while (testsToProcess.Count > 0)
        {
            var executing = new List<Task>();

            foreach (var notInParallelTestCases in GetOrderedTests())
            {
                var notInParallelTestCase = notInParallelTestCases.First();

                testsToProcess.Remove(notInParallelTestCase);

                var testWithResult = await ProcessTest(notInParallelTestCase.Test, true, session);
                
                executing.Add(testWithResult.ResultTask);
            }

            await WhenAllSafely(executing);

            List<IGrouping<ConstraintKeysCollection, NotInParallelTestCase>> GetOrderedTests()
            {
                return testsToProcess
                    .OrderBy(x => x.Test.Order)
                    .GroupBy(x => new ConstraintKeysCollection(x.Test.NotInParallelConstraintKeys!))
                    .ToList();
            }
        }
    }

    private async Task ProcessTests(Queue<TestInformation> queue, bool runInParallel, TestSessionContext session)
    {
        var executing = new List<Task>();
        
        await foreach (var testWithResult in ProcessQueue(queue, runInParallel, session))
        {
            executing.Add(testWithResult.ResultTask);
        }

        await WhenAllSafely(executing);
    }

    private async IAsyncEnumerable<TestWithResult> ProcessQueue(Queue<TestInformation> queue, bool runInParallel,
        TestSessionContext session)
    {
        while (queue.Count > 0)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            
            if (_currentlyExecutingTests < 1 || !_systemResourceMonitor.IsSystemStrained())
            {
                var test = queue.Dequeue();

                yield return await ProcessTest(test, runInParallel, session);
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task<TestWithResult> ProcessTest(TestInformation test, bool runInParallel, TestSessionContext session)
    {
        NotifyTestStart();
        
        var executionTask = _singleTestExecutor.ExecuteTestAsync(test, session);

        _ = executionTask.ContinueWith(_ =>
        {
            NotifyTestEnd();
        });

        if (!runInParallel)
        {
            await executionTask;
        }
        
        return new TestWithResult(test, executionTask);
    }

    private void NotifyTestEnd()
    {
        Interlocked.Decrement(ref _currentlyExecutingTests);
    }

    private void NotifyTestStart()
    {
        Interlocked.Increment(ref _currentlyExecutingTests);
    }

    private async Task WhenAllSafely(IEnumerable<Task> tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            await _logger.LogErrorAsync(e);
        }
    }
}