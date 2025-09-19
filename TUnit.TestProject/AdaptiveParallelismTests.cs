using System;
using System.Threading;
using System.Threading.Tasks;

namespace TUnit.TestProject;

public class AdaptiveParallelismTests
{
    private static int _currentlyRunning;
    private static int _maxConcurrent;
    private static readonly object _lock = new();

    [Test]
    [Repeat(1000)]
    public async Task IoIntensiveTest()
    {
        // Track concurrency
        lock (_lock)
        {
            _currentlyRunning++;
            if (_currentlyRunning > _maxConcurrent)
            {
                _maxConcurrent = _currentlyRunning;
            }
        }

        try
        {
            // Simulate I/O work
            await Task.Delay(1000);
        }
        finally
        {
            lock (_lock)
            {
                _currentlyRunning--;
            }
        }
    }

    [Test]
    [Repeat(500)]
    public async Task CpuIntensiveTest()
    {
        // Track concurrency
        lock (_lock)
        {
            _currentlyRunning++;
            if (_currentlyRunning > _maxConcurrent)
            {
                _maxConcurrent = _currentlyRunning;
            }
        }

        try
        {
            // Simulate CPU-intensive work
            var endTime = DateTime.UtcNow.AddMilliseconds(50);
            while (DateTime.UtcNow < endTime)
            {
                // Busy wait to consume CPU
                Thread.SpinWait(1000);
            }
            await Task.Yield();
        }
        finally
        {
            lock (_lock)
            {
                _currentlyRunning--;
            }
        }
    }

    [After(Class)]
    public static void ReportStats()
    {
        Console.WriteLine($"Max concurrent tests: {_maxConcurrent}");
        Console.WriteLine($"Processor count: {Environment.ProcessorCount}");

        // With adaptive parallelism, we expect max concurrent to be higher than processor count
        // for I/O-bound tests, as the system should detect low CPU usage and increase parallelism
        if (_maxConcurrent > Environment.ProcessorCount)
        {
            Console.WriteLine("Adaptive parallelism appears to be working - concurrency exceeded processor count!");
        }
    }
}
