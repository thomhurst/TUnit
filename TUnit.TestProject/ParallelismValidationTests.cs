using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

/// <summary>
/// Tests that validate basic parallel execution without any limiters
/// </summary>
public class UnconstrainedParallelTests
{
        private static readonly ConcurrentBag<(string TestName, DateTimeOffset Start, DateTimeOffset End)> _executionTimes = [];
        private static int _concurrentCount = 0;
        private static int _maxConcurrent = 0;
        private static readonly Lock _lock = new();

        [After(Test)]
        public async Task RecordExecution()
        {
            var context = TestContext.Current!;
            _executionTimes.Add((context.Metadata.TestDetails.TestName,
                                context.Execution.TestStart!.Value,
                                context.Execution.Result!.End!.Value));
            await Task.CompletedTask;
        }

        [After(Class)]
        public static async Task VerifyParallelExecution()
        {
            await Task.Delay(100); // Ensure all tests recorded

            var times = _executionTimes.ToArray();

            // Check we have all 16 tests (4 methods × 4 runs each with Repeat(3))
            await Assert.That(times.Length).IsEqualTo(16);

            // Check that tests overlapped (ran in parallel)
            var hadOverlap = false;
            for (int i = 0; i < times.Length && !hadOverlap; i++)
            {
                for (int j = i + 1; j < times.Length; j++)
                {
                    // Check if test j overlaps with test i
                    if (times[j].Start < times[i].End && times[i].Start < times[j].End)
                    {
                        hadOverlap = true;
                        break;
                    }
                }
            }

            await Assert.That(hadOverlap).IsTrue();
            await Assert.That(_maxConcurrent).IsGreaterThanOrEqualTo(2);
        }

        [Test, Repeat(3)]
        public async Task UnconstrainedTest1()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task UnconstrainedTest2()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task UnconstrainedTest3()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task UnconstrainedTest4()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        private static void TrackConcurrency()
        {
            var current = Interlocked.Increment(ref _concurrentCount);
            lock (_lock)
            {
                if (current > _maxConcurrent)
                {
                    _maxConcurrent = current;
                }
            }
            Thread.Sleep(50);
            Interlocked.Decrement(ref _concurrentCount);
        }
}

/// <summary>
/// Limit for LimitedParallelTests - allows 3 concurrent tests
/// </summary>
public class Limit3 : IParallelLimit
{
    public int Limit => 3;
}

/// <summary>
/// Tests that validate ParallelLimiter correctly limits concurrency to 3
/// </summary>
[ParallelLimiter<Limit3>]
public class LimitedParallelTests
{
        private static readonly ConcurrentBag<(string TestName, DateTimeOffset Start, DateTimeOffset End)> _executionTimes = [];
        private static int _concurrentCount = 0;
        private static int _maxConcurrent = 0;
        private static int _exceededLimit = 0;
        private static readonly Lock _lock = new();

        [After(Test)]
        public async Task RecordExecution()
        {
            var context = TestContext.Current!;
            _executionTimes.Add((context.Metadata.TestDetails.TestName,
                                context.Execution.TestStart!.Value,
                                context.Execution.Result!.End!.Value));
            await Task.CompletedTask;
        }

        [After(Class)]
        public static async Task VerifyLimitedParallelExecution()
        {
            await Task.Delay(100); // Ensure all tests recorded

            var times = _executionTimes.ToArray();

            // Check we have all 16 tests (4 methods × 4 runs each with Repeat(3))
            await Assert.That(times.Length).IsEqualTo(16);

            // Check that tests overlapped (ran in parallel)
            var hadOverlap = false;
            for (int i = 0; i < times.Length && !hadOverlap; i++)
            {
                for (int j = i + 1; j < times.Length; j++)
                {
                    if (times[j].Start < times[i].End && times[i].Start < times[j].End)
                    {
                        hadOverlap = true;
                        break;
                    }
                }
            }

            await Assert.That(hadOverlap).IsTrue();

            // Verify we ran in parallel (at least 2 concurrent)
            await Assert.That(_maxConcurrent).IsGreaterThanOrEqualTo(2);

            // Verify we never exceeded the limit of 3
            await Assert.That(_exceededLimit).IsEqualTo(0);
            await Assert.That(_maxConcurrent).IsLessThanOrEqualTo(3);
        }

        [Test, Repeat(3)]
        public async Task LimitedTest1()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task LimitedTest2()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task LimitedTest3()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task LimitedTest4()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        private static void TrackConcurrency()
        {
            var current = Interlocked.Increment(ref _concurrentCount);
            lock (_lock)
            {
                if (current > _maxConcurrent)
                {
                    _maxConcurrent = current;
                }
                if (current > 3) // Exceeds our limit
                {
                    _exceededLimit++;
                }
            }
            Thread.Sleep(50);
            Interlocked.Decrement(ref _concurrentCount);
        }
}

/// <summary>
/// Limit for StrictlySerialTests - allows only 1 test at a time
/// </summary>
public class Limit1 : IParallelLimit
{
    public int Limit => 1;
}

/// <summary>
/// Tests that validate ParallelLimiter with limit=1 forces serial execution
/// </summary>
[ParallelLimiter<Limit1>]
public class StrictlySerialTests
{
        private static readonly ConcurrentBag<(string TestName, DateTimeOffset Start, DateTimeOffset End)> _executionTimes = [];
        private static int _concurrentCount = 0;
        private static int _maxConcurrent = 0;
        private static int _exceededLimit = 0;
        private static readonly Lock _lock = new();

        [After(Test)]
        public async Task RecordExecution()
        {
            var context = TestContext.Current!;
            _executionTimes.Add((context.Metadata.TestDetails.TestName,
                                context.Execution.TestStart!.Value,
                                context.Execution.Result!.End!.Value));
            await Task.CompletedTask;
        }

        [After(Class)]
        public static async Task VerifySerialExecution()
        {
            await Task.Delay(100); // Ensure all tests recorded

            var times = _executionTimes.ToArray();

            // Check we have all 12 tests (4 methods × 3 runs each with Repeat(2))
            await Assert.That(times.Length).IsEqualTo(12);

            // With limit=1, no tests should overlap
            var hadOverlap = false;
            for (int i = 0; i < times.Length && !hadOverlap; i++)
            {
                for (int j = i + 1; j < times.Length; j++)
                {
                    if (times[j].Start < times[i].End && times[i].Start < times[j].End)
                    {
                        hadOverlap = true;
                        break;
                    }
                }
            }

            // Should NOT have overlap with limit=1
            await Assert.That(hadOverlap).IsFalse();

            // Verify we never exceeded the limit of 1
            await Assert.That(_exceededLimit).IsEqualTo(0);
            await Assert.That(_maxConcurrent).IsEqualTo(1);
        }

        [Test, Repeat(2)]
        public async Task SerialTest1()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(2)]
        public async Task SerialTest2()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(2)]
        public async Task SerialTest3()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(2)]
        public async Task SerialTest4()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        private static void TrackConcurrency()
        {
            var current = Interlocked.Increment(ref _concurrentCount);
            lock (_lock)
            {
                if (current > _maxConcurrent)
                {
                    _maxConcurrent = current;
                }
                if (current > 1) // Exceeds our limit
                {
                    _exceededLimit++;
                }
            }
            Thread.Sleep(50);
            Interlocked.Decrement(ref _concurrentCount);
        }
}

/// <summary>
/// Limit for HighParallelismTests - allows 10 concurrent tests
/// </summary>
public class Limit10 : IParallelLimit
{
    public int Limit => 10;
}

/// <summary>
/// Tests that validate ParallelLimiter with higher limit (10) allows high concurrency
/// </summary>
[ParallelLimiter<Limit10>]
public class HighParallelismTests
{
        private static readonly ConcurrentBag<(string TestName, DateTimeOffset Start, DateTimeOffset End)> _executionTimes = [];
        private static int _concurrentCount = 0;
        private static int _maxConcurrent = 0;
        private static readonly Lock _lock = new();

        [After(Test)]
        public async Task RecordExecution()
        {
            var context = TestContext.Current!;
            _executionTimes.Add((context.Metadata.TestDetails.TestName,
                                context.Execution.TestStart!.Value,
                                context.Execution.Result!.End!.Value));
            await Task.CompletedTask;
        }

        [After(Class)]
        public static async Task VerifyHighParallelExecution()
        {
            await Task.Delay(100); // Ensure all tests recorded

            var times = _executionTimes.ToArray();

            // Check we have all 16 tests (4 methods × 4 runs each with Repeat(3))
            await Assert.That(times.Length).IsEqualTo(16);

            // Check that tests overlapped significantly
            var hadOverlap = false;
            for (int i = 0; i < times.Length && !hadOverlap; i++)
            {
                for (int j = i + 1; j < times.Length; j++)
                {
                    if (times[j].Start < times[i].End && times[i].Start < times[j].End)
                    {
                        hadOverlap = true;
                        break;
                    }
                }
            }

            await Assert.That(hadOverlap).IsTrue();

            // With 12 tests and limit of 10, should see high concurrency
            await Assert.That(_maxConcurrent).IsGreaterThanOrEqualTo(4);
        }

        [Test, Repeat(3)]
        public async Task HighParallelTest1()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task HighParallelTest2()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task HighParallelTest3()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        [Test, Repeat(3)]
        public async Task HighParallelTest4()
        {
            TrackConcurrency();
            await Task.Delay(100);
        }

        private static void TrackConcurrency()
        {
            var current = Interlocked.Increment(ref _concurrentCount);
            lock (_lock)
            {
                if (current > _maxConcurrent)
                {
                    _maxConcurrent = current;
                }
            }
            Thread.Sleep(50);
            Interlocked.Decrement(ref _concurrentCount);
        }
}
