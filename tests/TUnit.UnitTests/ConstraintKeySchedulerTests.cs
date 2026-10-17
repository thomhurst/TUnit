using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

// Regression tests for #7075: synchronous tests with distinct [NotInParallel] keys must
// run concurrently. Each test blocks its thread on a barrier that only opens once every
// participant has started, so serialized execution times out instead of passing.
public class ConstraintKeySchedulerTests
{
    private static readonly TimeSpan BarrierTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task SynchronousTests_WithDistinctKeys_RunConcurrently()
    {
        var coordinator = new BlockingTestCoordinator(participantCount: 4);
        var scheduler = CreateScheduler(coordinator);

        var tests = Enumerable.Range(1, 4)
            .Select(i => Keyed($"distinct-{i}", priority: 0, $"key-{i}"))
            .ToArray();

        await scheduler.ExecuteTestsWithConstraintsAsync(tests, CancellationToken.None);

        await Assert.That(coordinator.TimedOutTests).IsEmpty();
        await Assert.That(coordinator.CompletedCount).IsEqualTo(4);
    }

    [Test]
    public async Task SynchronousWaiters_ReleasedByTheSameTest_RunConcurrently()
    {
        // The gate holds both keys, so both waiters queue and are released together from
        // the gate's key-release path. Neither waiter may run inline on the releasing
        // thread, or the other is never signalled while the first blocks.
        var coordinator = new BlockingTestCoordinator(participantCount: 2, nonBlockingTestId: "gate");
        var scheduler = CreateScheduler(coordinator);

        (AbstractExecutableTest, IReadOnlyList<string>, int)[] tests =
        [
            Keyed("gate", priority: 0, "key-1", "key-2"),
            Keyed("waiter-1", priority: 1, "key-1"),
            Keyed("waiter-2", priority: 1, "key-2")
        ];

        await scheduler.ExecuteTestsWithConstraintsAsync(tests, CancellationToken.None);

        await Assert.That(coordinator.TimedOutTests).IsEmpty();
        await Assert.That(coordinator.CompletedCount).IsEqualTo(3);
    }

    private static (AbstractExecutableTest, IReadOnlyList<string>, int) Keyed(string testId, int priority, params string[] keys)
        => (TestRunnerTests.CreateTest(testId), keys, priority);

    private static ConstraintKeyScheduler CreateScheduler(ITestCoordinator coordinator)
    {
        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new EmptyCommandLineOptions()));

        var runner = new TestRunner(
            coordinator,
            new TestRunnerTests.FakeMessageBus(),
            isFailFastEnabled: false,
            new CancellationTokenSource(),
            logger,
            new TestStateManager(),
            new ParallelLimitLockProvider(),
            new NotInParallelLock());

        return new ConstraintKeyScheduler(runner, logger, new HashSetPool());
    }

    // Runs each test body synchronously, as a non-async [Test] method would, and returns an
    // already-completed ValueTask so nothing in the pipeline yields on the test's behalf.
    private sealed class BlockingTestCoordinator(int participantCount, string? nonBlockingTestId = null) : ITestCoordinator
    {
        private readonly Barrier _barrier = new(participantCount);
        private readonly Lock _lock = new();
        private readonly List<string> _timedOutTests = [];
        private int _completedCount;

        public IReadOnlyList<string> TimedOutTests
        {
            get
            {
                lock (_lock)
                {
                    return [.. _timedOutTests];
                }
            }
        }

        public int CompletedCount => Volatile.Read(ref _completedCount);

        public ValueTask ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
        {
            if (test.TestId != nonBlockingTestId && !_barrier.SignalAndWait(BarrierTimeout, cancellationToken))
            {
                lock (_lock)
                {
                    _timedOutTests.Add(test.TestId);
                }
            }

            Interlocked.Increment(ref _completedCount);
            test.SetResult(TestState.Passed);
            return default;
        }
    }

    private sealed class EmptyCommandLineOptions : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName) => false;

        public bool TryGetOptionArgumentList(string optionName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string[]? arguments)
        {
            arguments = null;
            return false;
        }
    }
}
