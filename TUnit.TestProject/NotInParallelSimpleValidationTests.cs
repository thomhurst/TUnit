using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NotInParallelSimpleValidationTests
{
    private static readonly ConcurrentBag<(string TestName, DateTime StartTime, DateTime EndTime)> _executionRecords = [];
    private static readonly object _lock = new();
    private static int _currentlyRunning = 0;
    private static int _maxConcurrent = 0;

    [Before(Test)]
    public void RecordTestStart()
    {
        var testName = TestContext.Current!.TestDetails.TestName;
        
        lock (_lock)
        {
            _currentlyRunning++;
            if (_currentlyRunning > _maxConcurrent)
            {
                _maxConcurrent = _currentlyRunning;
            }
        }

        _executionRecords.Add((testName, DateTime.UtcNow, DateTime.MinValue));
    }

    [After(Test)]
    public void RecordTestEnd()
    {
        var testName = TestContext.Current!.TestDetails.TestName;
        var endTime = DateTime.UtcNow;
        
        lock (_lock)
        {
            _currentlyRunning--;
        }

        // Update the record with end time
        var records = _executionRecords.ToList();
        var recordToUpdate = records.FirstOrDefault(r => r.TestName == testName && r.EndTime == DateTime.MinValue);
        if (recordToUpdate != default)
        {
            _executionRecords.TryTake(out _); // Remove the old one
            _executionRecords.Add((testName, recordToUpdate.StartTime, endTime));
        }
    }

    [Test, NotInParallel]
    public async Task Test_A_ShouldRunFirst()
    {
        Console.WriteLine($"Test A started at {DateTime.UtcNow:HH:mm:ss.fff}");
        await Task.Delay(100);
        Console.WriteLine($"Test A finished at {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    [Test, NotInParallel]
    public async Task Test_B_ShouldRunAfterA()
    {
        Console.WriteLine($"Test B started at {DateTime.UtcNow:HH:mm:ss.fff}");
        await Task.Delay(100);
        Console.WriteLine($"Test B finished at {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    [Test, NotInParallel]
    public async Task Test_C_ShouldRunAfterB()
    {
        Console.WriteLine($"Test C started at {DateTime.UtcNow:HH:mm:ss.fff}");
        await Task.Delay(100);
        Console.WriteLine($"Test C finished at {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    [After(Class)]
    public static async Task ValidateSequentialExecution()
    {
        // Get all completed records
        var records = _executionRecords.Where(r => r.EndTime != DateTime.MinValue).ToArray();
        
        Console.WriteLine($"Maximum concurrent tests: {_maxConcurrent}");
        Console.WriteLine("Execution timeline:");
        
        foreach (var record in records.OrderBy(r => r.StartTime))
        {
            Console.WriteLine($"{record.TestName}: {record.StartTime:HH:mm:ss.fff} - {record.EndTime:HH:mm:ss.fff}");
        }

        // Validate that max concurrent was 1
        await Assert.That(_maxConcurrent)
            .IsEqualTo(1)
            .Because("Tests with [NotInParallel] and no constraint key should never run concurrently");

        // Validate that tests did not overlap
        var tolerance = TimeSpan.FromMilliseconds(50); // Allow for timing precision issues
        
        for (int i = 0; i < records.Length; i++)
        {
            for (int j = i + 1; j < records.Length; j++)
            {
                var test1 = records[i];
                var test2 = records[j];
                
                // Check if tests overlapped (one should end before the other starts, with tolerance)
                bool test1BeforeTest2 = test1.EndTime.Add(tolerance) <= test2.StartTime;
                bool test2BeforeTest1 = test2.EndTime.Add(tolerance) <= test1.StartTime;
                
                bool noOverlap = test1BeforeTest2 || test2BeforeTest1;
                
                await Assert.That(noOverlap)
                    .IsTrue()
                    .Because($"{test1.TestName} and {test2.TestName} should not overlap in execution time");
            }
        }
    }
}

// Tests in a different class to ensure cross-class sequential execution
[EngineTest(ExpectedResult.Pass)]  
public class NotInParallelSimpleValidationTests2
{
    [Test, NotInParallel]
    public async Task Test_D_ShouldRunSequentiallyWithOtherClass()
    {
        Console.WriteLine($"Test D started at {DateTime.UtcNow:HH:mm:ss.fff}");
        await Task.Delay(100);
        Console.WriteLine($"Test D finished at {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    [Test, NotInParallel]
    public async Task Test_E_ShouldRunSequentiallyWithOtherClass()
    {
        Console.WriteLine($"Test E started at {DateTime.UtcNow:HH:mm:ss.fff}");
        await Task.Delay(100);
        Console.WriteLine($"Test E finished at {DateTime.UtcNow:HH:mm:ss.fff}");
    }
}