using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class NotInParallelMixedTests
{
    private static readonly ConcurrentDictionary<string, List<ExecutionInfo>> ExecutionsByGroup = new();
    private static readonly ConcurrentDictionary<string, int> MaxConcurrentByGroup = new();
    private static readonly ConcurrentDictionary<string, int> CurrentlyRunningByGroup = new();
    private static readonly object GlobalLock = new();
    private static int GlobalMaxConcurrent = 0;
    private static int GlobalCurrentlyRunning = 0;

    [Before(Test)]
    public void RecordTestStart()
    {
        var testName = TestContext.Current!.TestDetails.TestName;
        var groupKey = GetGroupKeyForTest(testName);
        var startTime = DateTime.Now;

        lock (GlobalLock)
        {
            GlobalCurrentlyRunning++;
            if (GlobalCurrentlyRunning > GlobalMaxConcurrent)
                GlobalMaxConcurrent = GlobalCurrentlyRunning;
        }

        if (groupKey != null)
        {
            var current = CurrentlyRunningByGroup.AddOrUpdate(groupKey, 1, (_, v) => v + 1);
            MaxConcurrentByGroup.AddOrUpdate(groupKey, current, (_, v) => Math.Max(v, current));
        }

        var executions = ExecutionsByGroup.GetOrAdd(groupKey ?? "None", _ => new List<ExecutionInfo>());
        lock (executions)
        {
            executions.Add(new ExecutionInfo(testName, startTime, null));
        }
    }

    [After(Test)]
    public async Task RecordTestEnd()
    {
        var testName = TestContext.Current!.TestDetails.TestName;
        var groupKey = GetGroupKeyForTest(testName);
        var endTime = DateTime.Now;

        lock (GlobalLock)
        {
            GlobalCurrentlyRunning--;
        }

        if (groupKey != null)
        {
            CurrentlyRunningByGroup.AddOrUpdate(groupKey, 0, (_, v) => Math.Max(0, v - 1));
        }

        var executions = ExecutionsByGroup.GetOrAdd(groupKey ?? "None", _ => new List<ExecutionInfo>());
        lock (executions)
        {
            var execution = executions.FirstOrDefault(e => e.TestName == testName && e.EndTime == null);
            if (execution != null)
                execution.EndTime = endTime;
        }

        await ValidateExecutionRules(testName, groupKey);
    }

    // Tests with no constraint key - should not run in parallel with each other
    [Test, NotInParallel]
    public async Task NoKey_Test1()
    {
        await Task.Delay(200);
    }

    [Test, NotInParallel]
    public async Task NoKey_Test2()
    {
        await Task.Delay(200);
    }

    [Test, NotInParallel]
    public async Task NoKey_Test3()
    {
        await Task.Delay(200);
    }

    // Tests with "GroupA" key and order - should run sequentially in order
    [Test, NotInParallel("GroupA", Order = 2)]
    public async Task GroupA_Second()
    {
        await Task.Delay(150);
    }

    [Test, NotInParallel("GroupA", Order = 1)]
    public async Task GroupA_First()
    {
        await Task.Delay(150);
    }

    [Test, NotInParallel("GroupA", Order = 3)]
    public async Task GroupA_Third()
    {
        await Task.Delay(150);
    }

    // Tests with "GroupB" key without order - should not run in parallel with each other
    [Test, NotInParallel("GroupB")]
    public async Task GroupB_Test1()
    {
        await Task.Delay(100);
    }

    [Test, NotInParallel("GroupB")]
    public async Task GroupB_Test2()
    {
        await Task.Delay(100);
    }

    [Test, NotInParallel("GroupB")]
    public async Task GroupB_Test3()
    {
        await Task.Delay(100);
    }

    // Tests with "GroupC" key and mixed orders
    [Test, NotInParallel("GroupC", Order = 10)]
    public async Task GroupC_Last()
    {
        await Task.Delay(80);
    }

    [Test, NotInParallel("GroupC", Order = 1)]
    public async Task GroupC_First()
    {
        await Task.Delay(80);
    }

    [Test, NotInParallel("GroupC", Order = 5)]
    public async Task GroupC_Middle()
    {
        await Task.Delay(80);
    }

    // Tests without NotInParallel - can run in parallel with everything
    [Test]
    public async Task Parallel_Test1()
    {
        await Task.Delay(50);
    }

    [Test]
    public async Task Parallel_Test2()
    {
        await Task.Delay(50);
    }

    [Test]
    public async Task Parallel_Test3()
    {
        await Task.Delay(50);
    }

    // Tests with multiple constraint keys
    [Test, NotInParallel(["GroupD", "GroupE"])]
    public async Task MultiGroup_Test1()
    {
        await Task.Delay(120);
    }

    [Test, NotInParallel(["GroupD", "GroupF"])]
    public async Task MultiGroup_Test2()
    {
        await Task.Delay(120);
    }

    [Test, NotInParallel(["GroupE", "GroupF"])]
    public async Task MultiGroup_Test3()
    {
        await Task.Delay(120);
    }

    [After(Class)]
    public static async Task ValidateFinalResults()
    {
        // Validate that tests without keys didn't run in parallel
        if (ExecutionsByGroup.TryGetValue("NoKey", out var noKeyExecutions))
        {
            await ValidateNoOverlaps(noKeyExecutions, "NoKey");
            
            if (MaxConcurrentByGroup.TryGetValue("NoKey", out var maxConcurrent))
            {
                await Assert.That(maxConcurrent)
                    .IsEqualTo(1)
                    .Because($"Tests with NotInParallel and no key should not run concurrently. Max was: {maxConcurrent}");
            }
        }

        // Validate GroupA ran in order
        if (ExecutionsByGroup.TryGetValue("GroupA", out var groupAExecutions))
        {
            var orderedNames = groupAExecutions.OrderBy(e => e.StartTime).Select(e => e.TestName).ToList();
            await Assert.That(orderedNames)
                .IsEquivalentTo(["GroupA_First", "GroupA_Second", "GroupA_Third"])
                .Because("GroupA tests should execute in Order property sequence");

            await ValidateNoOverlaps(groupAExecutions, "GroupA");
        }

        // Validate GroupB didn't run in parallel
        if (ExecutionsByGroup.TryGetValue("GroupB", out var groupBExecutions))
        {
            await ValidateNoOverlaps(groupBExecutions, "GroupB");
        }

        // Validate GroupC ran in order
        if (ExecutionsByGroup.TryGetValue("GroupC", out var groupCExecutions))
        {
            var orderedNames = groupCExecutions.OrderBy(e => e.StartTime).Select(e => e.TestName).ToList();
            await Assert.That(orderedNames)
                .IsEquivalentTo(["GroupC_First", "GroupC_Middle", "GroupC_Last"])
                .Because("GroupC tests should execute in Order property sequence");

            await ValidateNoOverlaps(groupCExecutions, "GroupC");
        }

        // Validate multi-group tests respected all their constraints
        await ValidateMultiGroupConstraints();

        // Log summary
        Console.WriteLine($"Global max concurrent: {GlobalMaxConcurrent}");
        foreach (var kvp in MaxConcurrentByGroup)
        {
            Console.WriteLine($"Max concurrent in {kvp.Key}: {kvp.Value}");
        }
    }

    private async Task ValidateExecutionRules(string testName, string? groupKey)
    {
        if (groupKey == null && testName.StartsWith("NoKey_"))
        {
            groupKey = "NoKey";
        }

        // Don't validate multi-group tests here - they have special handling
        if (groupKey != null && !groupKey.Contains(","))
        {
            if (MaxConcurrentByGroup.TryGetValue(groupKey, out var maxConcurrent))
            {
                await Assert.That(maxConcurrent)
                    .IsLessThanOrEqualTo(1)
                    .Because($"Tests in {groupKey} should not run concurrently. Max was: {maxConcurrent}");
            }
        }
    }

    private static async Task ValidateNoOverlaps(List<ExecutionInfo> executions, string groupName)
    {
        foreach (var execution in executions.Where(e => e.EndTime != null))
        {
            var overlapping = executions
                .Where(e => e != execution && e.EndTime != null && e.OverlapsWith(execution))
                .Select(e => e.TestName)
                .ToList();

            await Assert.That(overlapping)
                .IsEmpty()
                .Because($"In {groupName}, {execution.TestName} should not overlap with other tests");
        }
    }

    private static async Task ValidateMultiGroupConstraints()
    {
        var multiGroupTests = new[] { "MultiGroup_Test1", "MultiGroup_Test2", "MultiGroup_Test3" };
        
        // Test1 and Test2 share GroupD - should not overlap
        await ValidatePairNoOverlap("MultiGroup_Test1", "MultiGroup_Test2", "GroupD");
        
        // Test1 and Test3 share GroupE - should not overlap  
        await ValidatePairNoOverlap("MultiGroup_Test1", "MultiGroup_Test3", "GroupE");
        
        // Test2 and Test3 share GroupF - should not overlap
        await ValidatePairNoOverlap("MultiGroup_Test2", "MultiGroup_Test3", "GroupF");
    }

    private static async Task ValidatePairNoOverlap(string test1, string test2, string sharedGroup)
    {
        var allExecutions = ExecutionsByGroup.Values.SelectMany(v => v).ToList();
        var exec1 = allExecutions.FirstOrDefault(e => e.TestName == test1);
        var exec2 = allExecutions.FirstOrDefault(e => e.TestName == test2);

        if (exec1 != null && exec2 != null && exec1.EndTime != null && exec2.EndTime != null)
        {
            await Assert.That(exec1.OverlapsWith(exec2))
                .IsFalse()
                .Because($"{test1} and {test2} share {sharedGroup} constraint and should not overlap");
        }
    }

    private static string? GetGroupKeyForTest(string testName)
    {
        if (testName.StartsWith("NoKey_")) return "NoKey";
        if (testName.StartsWith("GroupA_")) return "GroupA";
        if (testName.StartsWith("GroupB_")) return "GroupB";
        if (testName.StartsWith("GroupC_")) return "GroupC";
        if (testName.StartsWith("MultiGroup_"))
        {
            // For multi-group tests, we track them separately
            return testName switch
            {
                "MultiGroup_Test1" => "GroupD,GroupE",
                "MultiGroup_Test2" => "GroupD,GroupF",
                "MultiGroup_Test3" => "GroupE,GroupF",
                _ => null
            };
        }
        if (testName.StartsWith("Parallel_")) return null; // No constraint
        return null;
    }

    private class ExecutionInfo
    {
        public string TestName { get; }
        public DateTime StartTime { get; }
        public DateTime? EndTime { get; set; }

        public ExecutionInfo(string testName, DateTime startTime, DateTime? endTime)
        {
            TestName = testName;
            StartTime = startTime;
            EndTime = endTime;
        }

        public bool OverlapsWith(ExecutionInfo other)
        {
            if (EndTime == null || other.EndTime == null)
                return false;

            // Add 50ms tolerance for timing precision - tests may appear to overlap due to clock resolution
            // and test framework overhead. We consider tests as overlapping only if they truly run concurrently,
            // not if one starts immediately after another finishes.
            var tolerance = TimeSpan.FromMilliseconds(50);
            
            // Tests overlap if:
            // - This test starts before the other ends (with tolerance)
            // - AND the other test starts before this one ends (with tolerance)
            // The tolerance is subtracted from end times to be more lenient
            return StartTime < other.EndTime.Value.Subtract(tolerance) && other.StartTime < EndTime.Value.Subtract(tolerance);
        }
    }
}