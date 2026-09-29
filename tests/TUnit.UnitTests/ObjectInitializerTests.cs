using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

public class ObjectInitializerTests
{
    // Only bound how long a regression can hang the suite - passing runs never wait for them.
    // The prefix gate must outlast HangTimeout, or a blocked caller would be released before it is detected.
    private static readonly TimeSpan HangTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PrefixGateTimeout = TimeSpan.FromSeconds(60);

    // https://github.com/thomhurst/TUnit/issues/6904
    [Test]
    public async Task Waiting_Caller_Does_Not_Block_While_InitializeAsync_Runs_Synchronously()
    {
        using var fixture = new BlockingPrefixInitializer();
        var initialization = Task.Run(() => ObjectInitializer.InitializeAsync(fixture).AsTask());
        var callReturned = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task waiter = Task.CompletedTask;
        bool callReturnedWhilePrefixBlocked;
        bool completedWhilePrefixBlocked;

        try
        {
            await fixture.PrefixEntered.Task.WaitAsync(HangTimeout);

            waiter = Task.Run(() =>
            {
                var pending = ObjectInitializer.InitializeAsync(fixture);
                callReturned.SetResult(true);
                return pending.AsTask();
            });

            callReturnedWhilePrefixBlocked = await Task.WhenAny(callReturned.Task, Task.Delay(HangTimeout)) == callReturned.Task;
            completedWhilePrefixBlocked = waiter.IsCompleted;
        }
        finally
        {
            fixture.ReleasePrefix();
        }

        await Task.WhenAll(initialization, waiter).WaitAsync(HangTimeout);

        await Assert.That(callReturnedWhilePrefixBlocked).IsTrue();
        await Assert.That(completedWhilePrefixBlocked).IsFalse();
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    public async Task InitializeAsync_Runs_Once_Under_Contention()
    {
        var fixtures = Enumerable.Range(0, 100).Select(_ => new YieldingInitializer()).ToArray();

        var callers = fixtures
            .SelectMany(fixture => Enumerable.Range(0, 8).Select(_ => Task.Run(() => ObjectInitializer.InitializeAsync(fixture).AsTask())))
            .ToArray();
        await Task.WhenAll(callers).WaitAsync(HangTimeout);

        foreach (var fixture in fixtures)
        {
            await Assert.That(fixture.InitializeCount).IsEqualTo(1);
            await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
        }
    }

    [Test]
    [Arguments(false, true)]
    [Arguments(false, false)]
    [Arguments(true, true)]
    [Arguments(true, false)]
    public async Task Failure_Is_Cached_And_Rethrown_As_The_Same_Exception(bool cancellation, bool throwSynchronously)
    {
        Exception failure = cancellation ? new OperationCanceledException("initializer gave up") : new InvalidOperationException("initialization failed");
        var fixture = new ThrowingInitializer(failure, throwSynchronously);

        var first = ObjectInitializer.InitializeAsync(fixture).AsTask();
        var second = ObjectInitializer.InitializeAsync(fixture).AsTask();

        await Assert.That(await CaptureAsync(first)).IsSameReferenceAs(failure);
        await Assert.That(await CaptureAsync(second)).IsSameReferenceAs(failure);
        // An OperationCanceledException from the initializer still cancels callers' tasks, as before.
        await Assert.That(first.IsCanceled).IsEqualTo(cancellation);
        await Assert.That(second.IsCanceled).IsEqualTo(cancellation);
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Cancelling_The_Initializing_Caller_Does_Not_Poison_The_Result(bool initializationFails)
    {
        var fixture = new GatedInitializer();
        var failure = new InvalidOperationException("initialization failed");
        using var cancellationTokenSource = new CancellationTokenSource();
        var initializingCaller = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();
        var waiter = ObjectInitializer.InitializeAsync(fixture).AsTask();

        cancellationTokenSource.Cancel();

        await Assert.That(async () => await initializingCaller.WaitAsync(HangTimeout)).Throws<OperationCanceledException>();
        await Assert.That(waiter.IsCompleted).IsFalse();

        if (initializationFails)
        {
            fixture.Fail(failure);
            var observed = await Assert.That(async () => await waiter.WaitAsync(HangTimeout)).Throws<InvalidOperationException>();
            await Assert.That(observed).IsSameReferenceAs(failure);
        }
        else
        {
            fixture.Complete();
            await waiter.WaitAsync(HangTimeout);
        }

        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsEqualTo(!initializationFails);
    }

    [Test]
    public async Task Cancelling_A_Waiting_Caller_Does_Not_Cancel_The_Initialization()
    {
        var fixture = new GatedInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();
        var initializingCaller = ObjectInitializer.InitializeAsync(fixture).AsTask();
        var cancelledWaiter = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();

        cancellationTokenSource.Cancel();

        await Assert.That(async () => await cancelledWaiter.WaitAsync(HangTimeout)).Throws<OperationCanceledException>();
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();

        fixture.Complete();
        await initializingCaller.WaitAsync(HangTimeout);
        await ObjectInitializer.InitializeAsync(fixture);

        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Waiting_Continuations_Do_Not_Run_On_The_Initializing_Thread(bool cancellableWait)
    {
        // Arrange
        const int waiterCount = 4;
        using var fixture = new BlockingPrefixInitializer(completeSynchronously: true);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellableWait ? cancellationTokenSource.Token : CancellationToken.None;
        // Compare Thread objects, not ManagedThreadId: once the dedicated thread exits, the runtime
        // can hand its ID to a thread-pool thread that then runs a correctly queued continuation.
        Thread? initializingThread = null;
        Task<Thread>[] waiters = [];

        // A dedicated thread cannot later pick up correctly queued waiter continuations.
        var initialization = Task.Factory.StartNew(() =>
        {
            initializingThread = Thread.CurrentThread;
            return ObjectInitializer.InitializeAsync(fixture).AsTask();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

        async Task<Thread> ObserveContinuationAsync()
        {
            await ObjectInitializer.InitializeAsync(fixture, cancellationToken).ConfigureAwait(false);
            return Thread.CurrentThread;
        }

        // Act
        try
        {
            await fixture.PrefixEntered.Task.WaitAsync(HangTimeout);
            waiters = Enumerable.Range(0, waiterCount).Select(_ => ObserveContinuationAsync()).ToArray();
        }
        finally
        {
            fixture.ReleasePrefix();
            await initialization.WaitAsync(HangTimeout);
        }

        var continuationThreads = await Task.WhenAll(waiters).WaitAsync(HangTimeout);

        // Assert
        await Assert.That(continuationThreads.Length).IsEqualTo(waiterCount);
        await Assert.That(initializingThread).IsNotNull();
        foreach (var thread in continuationThreads)
        {
            await Assert.That(thread).IsNotSameReferenceAs(initializingThread);
        }
    }

    private static async Task<Exception?> CaptureAsync(Task task)
    {
        try
        {
            await task.WaitAsync(HangTimeout);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Blocks inside the synchronous part of InitializeAsync (before its first await), like
    /// sync-over-async code in a third-party constructor would.
    /// </summary>
    private sealed class BlockingPrefixInitializer(bool completeSynchronously = false) : IAsyncInitializer, IDisposable
    {
        private readonly ManualResetEventSlim _prefixGate = new();
        private int _initializeCount;

        public TaskCompletionSource<bool> PrefixEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public void ReleasePrefix() => _prefixGate.Set();

        public async Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            PrefixEntered.TrySetResult(true);
            _prefixGate.Wait(PrefixGateTimeout);
            if (!completeSynchronously)
            {
                await Task.Yield();
            }
        }

        public void Dispose() => _prefixGate.Dispose();
    }

    /// <summary>
    /// Suspends until <see cref="Complete"/> or <see cref="Fail"/> is called.
    /// </summary>
    private sealed class GatedInitializer : IAsyncInitializer
    {
        private readonly TaskCompletionSource<bool> _gate = new();
        private int _initializeCount;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public void Complete() => _gate.SetResult(true);

        public void Fail(Exception exception) => _gate.SetException(exception);

        public async Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            await _gate.Task;
        }
    }

    private sealed class YieldingInitializer : IAsyncInitializer
    {
        private int _initializeCount;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public async Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            await Task.Yield();
        }
    }

    private sealed class ThrowingInitializer(Exception exception, bool throwSynchronously) : IAsyncInitializer
    {
        private int _initializeCount;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            return throwSynchronously ? throw exception : ThrowAfterYieldAsync();
        }

        private async Task ThrowAfterYieldAsync()
        {
            await Task.Yield();
            throw exception;
        }
    }
}
