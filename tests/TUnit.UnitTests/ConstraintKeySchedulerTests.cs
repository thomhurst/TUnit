using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;
using MtpLogLevel = Microsoft.Testing.Platform.Logging.LogLevel;

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
        // The gate holds both keys until both waiters are queued behind it, so both are
        // released together from the gate's key-release path. Neither waiter may run inline
        // on the releasing thread, or the other is never signalled while the first blocks.
        var bothWaitersQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new BlockingTestCoordinator(participantCount: 2, nonBlockingTestId: "gate", nonBlockingTestGate: bothWaitersQueued.Task);
        var outputDevice = new TraceOutputDevice(text =>
        {
            if (text.StartsWith("Queueing test waiter-2 ", StringComparison.Ordinal))
            {
                bothWaitersQueued.TrySetResult();
            }

            return Task.CompletedTask;
        });
        var scheduler = CreateScheduler(coordinator, outputDevice);

        (AbstractExecutableTest, IReadOnlyList<string>, int)[] tests =
        [
            Keyed("gate", priority: 0, "key-1", "key-2"),
            Keyed("waiter-1", priority: 1, "key-1"),
            Keyed("waiter-2", priority: 1, "key-2")
        ];

        await scheduler.ExecuteTestsWithConstraintsAsync(tests, CancellationToken.None);

        await Assert.That(coordinator.TimedOutTests).IsEmpty();
        await Assert.That(coordinator.CompletedCount).IsEqualTo(3);
        await Assert.That(outputDevice.Contains("Unblocking waiting test waiter-1 ")).IsTrue();
        await Assert.That(outputDevice.Contains("Unblocking waiting test waiter-2 ")).IsTrue();
    }

    [Test]
    public async Task SynchronousWaiter_ReleasedBeforeItAwaits_DoesNotBlockLaterTests()
    {
        // The gate finishes and frees the waiter's key after the scheduling loop queues the
        // waiter but before the loop awaits the waiter's start signal. The waiter must still
        // leave the loop's thread, or "later" is never started and the barrier times out.
        // The gate holds its key until the waiter is queued, so the waiter always takes the
        // queued path regardless of how quickly the thread pool runs the gate.
        (AbstractExecutableTest Test, IReadOnlyList<string>, int)[] tests =
        [
            Keyed("gate", priority: 0, "key-1"),
            Keyed("waiter", priority: 1, "key-1"),
            Keyed("later", priority: 2, "key-2")
        ];
        var gate = tests[0].Test;

        var waiterQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new BlockingTestCoordinator(participantCount: 2, nonBlockingTestId: "gate", nonBlockingTestGate: waiterQueued.Task);
        var outputDevice = new TraceOutputDevice(text =>
        {
            if (!text.StartsWith("Queueing test waiter ", StringComparison.Ordinal))
            {
                return Task.CompletedTask;
            }

            // Runs on the scheduling loop between queueing the waiter and awaiting its signal:
            // let the gate finish, and hold the loop until it has released the waiter's key.
            waiterQueued.TrySetResult();
            return gate.ExecutionTask!;
        });
        var scheduler = CreateScheduler(coordinator, outputDevice);

        await scheduler.ExecuteTestsWithConstraintsAsync(tests, CancellationToken.None);

        await Assert.That(coordinator.TimedOutTests).IsEmpty();
        await Assert.That(coordinator.CompletedCount).IsEqualTo(3);
        await Assert.That(outputDevice.Contains("Unblocking waiting test waiter ")).IsTrue();
    }

    private static (AbstractExecutableTest, IReadOnlyList<string>, int) Keyed(string testId, int priority, params string[] keys)
        => (TestRunnerTests.CreateTest(testId), keys, priority);

    private static ConstraintKeyScheduler CreateScheduler(ITestCoordinator coordinator, TraceOutputDevice? outputDevice = null)
    {
        var logger = outputDevice is null
            ? new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new CommandLineOptions(logLevel: null)))
            : new TUnitFrameworkLogger(null!, outputDevice, new NoOpLogger(), new LogLevelProvider(new CommandLineOptions(logLevel: "Trace")));

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
    // The non-blocking test skips the barrier and optionally waits for a gate first.
    private sealed class BlockingTestCoordinator(
        int participantCount,
        string? nonBlockingTestId = null,
        Task? nonBlockingTestGate = null) : ITestCoordinator
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
            if (test.TestId == nonBlockingTestId)
            {
                return nonBlockingTestGate is null
                    ? Complete(test)
                    : CompleteAfterGateAsync(test, nonBlockingTestGate, cancellationToken);
            }

            if (!_barrier.SignalAndWait(BarrierTimeout, cancellationToken))
            {
                lock (_lock)
                {
                    _timedOutTests.Add(test.TestId);
                }
            }

            return Complete(test);
        }

        private async ValueTask CompleteAfterGateAsync(AbstractExecutableTest test, Task gate, CancellationToken cancellationToken)
        {
            try
            {
                await gate.WaitAsync(BarrierTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                lock (_lock)
                {
                    _timedOutTests.Add(test.TestId);
                }
            }

            await Complete(test).ConfigureAwait(false);
        }

        private ValueTask Complete(AbstractExecutableTest test)
        {
            Interlocked.Increment(ref _completedCount);
            test.SetResult(TestState.Passed);
            return default;
        }
    }

    // Receives the scheduler's trace messages, letting a test act at a known point in the
    // scheduling loop (for example, between queueing a waiter and awaiting its signal).
    private sealed class TraceOutputDevice(Func<string, Task> onMessage) : IOutputDevice
    {
        private readonly Lock _lock = new();
        private readonly List<string> _messages = [];

        public bool Contains(string prefix)
        {
            lock (_lock)
            {
                return _messages.Exists(message => message.StartsWith(prefix, StringComparison.Ordinal));
            }
        }

        public Task DisplayAsync(IOutputDeviceDataProducer producer, IOutputDeviceData data, CancellationToken cancellationToken)
        {
            if (data is not FormattedTextOutputDeviceData textData)
            {
                return Task.CompletedTask;
            }

            lock (_lock)
            {
                _messages.Add(textData.Text);
            }

            return onMessage(textData.Text);
        }
    }

    private sealed class NoOpLogger : Microsoft.Testing.Platform.Logging.ILogger
    {
        public Task LogAsync<TState>(MtpLogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Task.CompletedTask;

        public void Log<TState>(MtpLogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        public bool IsEnabled(MtpLogLevel logLevel) => false;
    }

    private sealed class CommandLineOptions(string? logLevel) : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName)
            => logLevel is not null && optionName == LogLevelCommandProvider.LogLevelOption;

        public bool TryGetOptionArgumentList(string optionName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string[]? arguments)
        {
            if (IsOptionSet(optionName))
            {
                arguments = [logLevel!];
                return true;
            }

            arguments = null;
            return false;
        }
    }
}
