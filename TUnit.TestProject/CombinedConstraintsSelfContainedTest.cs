using System.Collections.Concurrent;

namespace TUnit.TestProject;

// A self-contained test to verify combined ParallelGroup + NotInParallel constraints work correctly

public static class CombinedConstraintTracker
{
    public static readonly ConcurrentBag<(string TestName, DateTime Start, DateTime End, string Group, string Key)> ExecutionLog = [];
    public static readonly object Lock = new();
}

[ParallelGroup("Group1")]
[NotInParallel("Key1")]
public class CombinedConstraintTest1
{
    [Test]
    public async Task Test1A()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test1A", start, end, "Group1", "Key1"));
        }
    }
    
    [Test]
    public async Task Test1B()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test1B", start, end, "Group1", "Key1"));
        }
    }
}

[ParallelGroup("Group1")]
[NotInParallel("Key2")]
public class CombinedConstraintTest2
{
    [Test]
    public async Task Test2A()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test2A", start, end, "Group1", "Key2"));
        }
    }
    
    [Test]
    public async Task Test2B()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test2B", start, end, "Group1", "Key2"));
        }
    }
}

[ParallelGroup("Group2")]
public class CombinedConstraintTest3
{
    [Test]
    public async Task Test3A()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test3A", start, end, "Group2", "None"));
        }
    }
    
    [Test]
    public async Task Test3B()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(200);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraintTracker.Lock)
        {
            CombinedConstraintTracker.ExecutionLog.Add(("Test3B", start, end, "Group2", "None"));
        }
    }
}

public class CombinedConstraintVerifier
{
    [Test]
    [DependsOn(typeof(CombinedConstraintTest1))]
    [DependsOn(typeof(CombinedConstraintTest2))]
    [DependsOn(typeof(CombinedConstraintTest3))]
    public async Task VerifyConstraintsCombineCorrectly()
    {
        // Wait a bit to ensure all tests have completed
        await Task.Delay(100);
        
        var log = CombinedConstraintTracker.ExecutionLog.OrderBy(x => x.Start).ToList();
        
        // We should have 6 test executions
        await Assert.That(log).HasCount().EqualTo(6);
        
        // 1. Tests with same key should not overlap
        var key1Tests = log.Where(x => x.Key == "Key1").ToList();
        for (int i = 0; i < key1Tests.Count - 1; i++)
        {
            var noOverlap = key1Tests[i].End <= key1Tests[i + 1].Start;
            await Assert.That(noOverlap)
                .IsTrue()
                .Because($"Key1 tests should not overlap: {key1Tests[i].TestName} and {key1Tests[i + 1].TestName}");
        }
        
        var key2Tests = log.Where(x => x.Key == "Key2").ToList();
        for (int i = 0; i < key2Tests.Count - 1; i++)
        {
            var noOverlap = key2Tests[i].End <= key2Tests[i + 1].Start;
            await Assert.That(noOverlap)
                .IsTrue()
                .Because($"Key2 tests should not overlap: {key2Tests[i].TestName} and {key2Tests[i + 1].TestName}");
        }
        
        // 2. Tests with different keys in same group CAN overlap
        if (key1Tests.Any() && key2Tests.Any())
        {
            var key1Range = (Start: key1Tests.Min(t => t.Start), End: key1Tests.Max(t => t.End));
            var key2Range = (Start: key2Tests.Min(t => t.Start), End: key2Tests.Max(t => t.End));
            
            // They should be able to overlap (but don't have to)
            Console.WriteLine($"Key1 range: {key1Range.Start:HH:mm:ss.fff} - {key1Range.End:HH:mm:ss.fff}");
            Console.WriteLine($"Key2 range: {key2Range.Start:HH:mm:ss.fff} - {key2Range.End:HH:mm:ss.fff}");
        }
        
        // 3. Different groups should NOT overlap
        var group1Tests = log.Where(x => x.Group == "Group1").ToList();
        var group2Tests = log.Where(x => x.Group == "Group2").ToList();
        
        if (group1Tests.Any() && group2Tests.Any())
        {
            var group1Range = (Start: group1Tests.Min(t => t.Start), End: group1Tests.Max(t => t.End));
            var group2Range = (Start: group2Tests.Min(t => t.Start), End: group2Tests.Max(t => t.End));
            
            var noOverlap = group1Range.End <= group2Range.Start || group2Range.End <= group1Range.Start;
            await Assert.That(noOverlap)
                .IsTrue()
                .Because($"Different parallel groups should not overlap. Group1: {group1Range.Start:HH:mm:ss.fff}-{group1Range.End:HH:mm:ss.fff}, Group2: {group2Range.Start:HH:mm:ss.fff}-{group2Range.End:HH:mm:ss.fff}");
        }
        
        // 4. Tests in Group2 (no NotInParallel) can run in parallel
        var group2OnlyTests = log.Where(x => x.Group == "Group2" && x.Key == "None").ToList();
        if (group2OnlyTests.Count > 1)
        {
            // Check if any tests overlap (they should be allowed to)
            Console.WriteLine($"Group2 tests without NotInParallel constraint:");
            foreach (var test in group2OnlyTests)
            {
                Console.WriteLine($"  {test.TestName}: {test.Start:HH:mm:ss.fff} - {test.End:HH:mm:ss.fff}");
            }
        }
        
        Console.WriteLine("\n✅ Combined ParallelGroup + NotInParallel constraints work correctly!");
    }
}