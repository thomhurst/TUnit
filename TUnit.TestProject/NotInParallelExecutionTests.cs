using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class NotInParallelExecutionTests
{
    private static readonly ConcurrentBag<TestExecutionRecord> ExecutionRecords = [];
    private static readonly object Lock = new();
    private static int CurrentlyRunning = 0;
    private static int MaxConcurrentTests = 0;

    [Before(Test)]
    public void RecordTestStart()
    {
        lock (Lock)
        {
            CurrentlyRunning++;
            if (CurrentlyRunning > MaxConcurrentTests)
            {
                MaxConcurrentTests = CurrentlyRunning;
            }
        }
        
        ExecutionRecords.Add(new TestExecutionRecord(
            TestContext.Current!.TestDetails.TestName,
            TestContext.Current.TestStart.DateTime,
            null,
            CurrentlyRunning
        ));
    }

    [After(Test)]
    public async Task RecordTestEnd()
    {
        lock (Lock)
        {
            CurrentlyRunning--;
        }

        var record = ExecutionRecords.FirstOrDefault(r => 
            r.TestName == TestContext.Current!.TestDetails.TestName && 
            r.EndTime == null);
        
        if (record != null)
        {
            record.EndTime = TestContext.Current.Result!.End!.Value.DateTime;
        }

        await AssertNoParallelExecution();
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_ExecutionTest1()
    {
        await Task.Delay(300);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_ExecutionTest2()
    {
        await Task.Delay(300);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_ExecutionTest3()
    {
        await Task.Delay(300);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_ExecutionTest4()
    {
        await Task.Delay(300);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_ExecutionTest5()
    {
        await Task.Delay(300);
    }

    private async Task AssertNoParallelExecution()
    {
        await Assert.That(MaxConcurrentTests)
            .IsEqualTo(1)
            .Because($"Tests with NotInParallel should not run concurrently. Max concurrent: {MaxConcurrentTests}");

        var completedRecords = ExecutionRecords.Where(r => r.EndTime != null).ToList();
        
        foreach (var record in completedRecords)
        {
            var overlappingTests = completedRecords
                .Where(r => r != record && r.OverlapsWith(record))
                .Select(r => r.TestName)
                .ToList();

            await Assert.That(overlappingTests)
                .IsEmpty()
                .Because($"Test {record.TestName} overlapped with: {string.Join(", ", overlappingTests)}");
        }
    }

    private class TestExecutionRecord
    {
        public string TestName { get; }
        public DateTime StartTime { get; }
        public DateTime? EndTime { get; set; }
        public int ConcurrentCount { get; }

        public TestExecutionRecord(string testName, DateTime startTime, DateTime? endTime, int concurrentCount)
        {
            TestName = testName;
            StartTime = startTime;
            EndTime = endTime;
            ConcurrentCount = concurrentCount;
        }

        public bool OverlapsWith(TestExecutionRecord other)
        {
            if (EndTime == null || other.EndTime == null)
            {
                return false;
            }

            return StartTime < other.EndTime.Value && other.StartTime < EndTime.Value;
        }
    }
}