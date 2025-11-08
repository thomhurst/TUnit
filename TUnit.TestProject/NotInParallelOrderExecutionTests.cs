using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class NotInParallelOrderExecutionTests
{
    private static readonly ConcurrentBag<OrderedExecutionRecord> OrderedExecutionRecords = [];
    private static readonly ConcurrentDictionary<string, List<string>> ExecutionOrderByGroup = new();
    private static readonly ConcurrentDictionary<string, object> GroupLocks = new();
    private static readonly ConcurrentDictionary<string, int> CurrentlyExecutingPerGroup = new();
    private static readonly ConcurrentDictionary<string, int> MaxConcurrentPerGroup = new();

    [Before(Test)]
    public void RecordOrderedTestStart()
    {
        var testName = TestContext.Current!.Metadata.TestDetails.TestName;
        var groupKey = GetGroupKey(testName);

        var groupLock = GroupLocks.GetOrAdd(groupKey, new object());
        lock (groupLock)
        {
            var current = CurrentlyExecutingPerGroup.AddOrUpdate(groupKey, 1, (_, v) => v + 1);
            MaxConcurrentPerGroup.AddOrUpdate(groupKey, current, (_, v) => Math.Max(v, current));

            var orderList = ExecutionOrderByGroup.GetOrAdd(groupKey, new List<string>());
            orderList.Add(testName);
        }

        // Use TestStart if available, otherwise use DateTime.Now
        var startTime = TestContext.Current.Execution.TestStart?.DateTime ?? DateTime.Now;

        OrderedExecutionRecords.Add(new OrderedExecutionRecord(
            testName,
            groupKey,
            startTime,
            null
        ));
    }

    [After(Test)]
    public async Task RecordOrderedTestEnd()
    {
        var testName = TestContext.Current!.Metadata.TestDetails.TestName;
        var groupKey = GetGroupKey(testName);

        var groupLock = GroupLocks.GetOrAdd(groupKey, new object());
        lock (groupLock)
        {
            CurrentlyExecutingPerGroup.AddOrUpdate(groupKey, 0, (_, v) => Math.Max(0, v - 1));
        }

        var record = OrderedExecutionRecords.FirstOrDefault(r =>
            r.TestName == testName &&
            r.EndTime == null);

        if (record != null)
        {
            // Use Result.End if available, otherwise use DateTime.Now
            record.EndTime = TestContext.Current.Execution.Result?.End?.DateTime ?? DateTime.Now;
        }

        await AssertOrderedExecutionWithinGroup(groupKey);
    }

    [Test, NotInParallel("OrderGroup1", Order = 3)]
    public async Task OrderedTest_Third()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Test, NotInParallel("OrderGroup1", Order = 1)]
    public async Task OrderedTest_First()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Test, NotInParallel("OrderGroup1", Order = 5)]
    public async Task OrderedTest_Fifth()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Test, NotInParallel("OrderGroup1", Order = 2)]
    public async Task OrderedTest_Second()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Test, NotInParallel("OrderGroup1", Order = 4)]
    public async Task OrderedTest_Fourth()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Test, NotInParallel("OrderGroup2", Order = 2)]
    public async Task OrderedGroup2_Second()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(150));
    }

    [Test, NotInParallel("OrderGroup2", Order = 1)]
    public async Task OrderedGroup2_First()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(150));
    }

    [Test, NotInParallel("OrderGroup2", Order = 3)]
    public async Task OrderedGroup2_Third()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(150));
    }

    [After(Class)]
    public static async Task VerifyExecutionOrder()
    {
        if (ExecutionOrderByGroup.TryGetValue("OrderGroup1", out var group1Tests) && group1Tests.Count == 5)
        {
            await Assert.That(group1Tests)
                .IsEquivalentTo([
                    "OrderedTest_First",
                    "OrderedTest_Second",
                    "OrderedTest_Third",
                    "OrderedTest_Fourth",
                    "OrderedTest_Fifth"
                ])
                .Because("Tests in OrderGroup1 should execute in order defined by Order property");
        }

        if (ExecutionOrderByGroup.TryGetValue("OrderGroup2", out var group2Tests) && group2Tests.Count == 3)
        {
            await Assert.That(group2Tests)
                .IsEquivalentTo([
                    "OrderedGroup2_First",
                    "OrderedGroup2_Second",
                    "OrderedGroup2_Third"
                ])
                .Because("Tests in OrderGroup2 should execute in order defined by Order property");
        }

        // Verify that tests from different groups could run in parallel
        var allRecords = OrderedExecutionRecords.Where(r => r.EndTime != null).ToList();
        var group1Records = allRecords.Where(r => r.GroupKey == "OrderGroup1").ToList();
        var group2Records = allRecords.Where(r => r.GroupKey == "OrderGroup2").ToList();

        if (group1Records.Any() && group2Records.Any())
        {
            var group1Start = group1Records.Min(r => r.StartTime);
            var group1End = group1Records.Max(r => r.EndTime!.Value);
            var group2Start = group2Records.Min(r => r.StartTime);
            var group2End = group2Records.Max(r => r.EndTime!.Value);

            // Check if the groups overlapped in time (which is allowed and expected)
            var overlapped = group1Start < group2End && group2Start < group1End;
            Console.WriteLine($"Groups ran in parallel: {overlapped}");
        }
    }

    private async Task AssertOrderedExecutionWithinGroup(string groupKey)
    {
        // Check that within this group, tests did not run in parallel
        if (MaxConcurrentPerGroup.TryGetValue(groupKey, out var maxConcurrent))
        {
            await Assert.That(maxConcurrent)
                .IsEqualTo(1)
                .Because($"Tests within {groupKey} should not run concurrently. Max concurrent: {maxConcurrent}");
        }

        // Check for overlaps within the same group
        var completedRecords = OrderedExecutionRecords
            .Where(r => r.EndTime != null && r.GroupKey == groupKey)
            .ToList();

        foreach (var record in completedRecords)
        {
            var overlappingTests = completedRecords
                .Where(r => r != record && r.OverlapsWith(record))
                .Select(r => r.TestName)
                .ToList();

            await Assert.That(overlappingTests)
                .IsEmpty()
                .Because($"In {groupKey}, test {record.TestName} overlapped with: {string.Join(", ", overlappingTests)}");
        }
    }

    private static string GetGroupKey(string testName)
    {
        if (testName.StartsWith("OrderedTest_"))
        {
            return "OrderGroup1";
        }
        if (testName.StartsWith("OrderedGroup2_"))
        {
            return "OrderGroup2";
        }
        return "Unknown";
    }

    private class OrderedExecutionRecord
    {
        public string TestName { get; }
        public string GroupKey { get; }
        public DateTime StartTime { get; }
        public DateTime? EndTime { get; set; }

        public OrderedExecutionRecord(string testName, string groupKey, DateTime startTime, DateTime? endTime)
        {
            TestName = testName;
            GroupKey = groupKey;
            StartTime = startTime;
            EndTime = endTime;
        }

        public bool OverlapsWith(OrderedExecutionRecord other)
        {
            if (EndTime == null || other.EndTime == null)
            {
                return false;
            }

            return StartTime < other.EndTime.Value && other.StartTime < EndTime.Value;
        }
    }
}
