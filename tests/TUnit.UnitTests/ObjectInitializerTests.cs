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
        var fixture = new BlockingPrefixInitializer();
        var initialization = Task.Run(() => ObjectInitializer.InitializeAsync(fixture).AsTask());
        await fixture.PrefixEntered.Task.WaitAsync(HangTimeout);

        var callReturned = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = Task.Run(() =>
        {
            var pending = ObjectInitializer.InitializeAsync(fixture);
            callReturned.SetResult(true);
            return pending.AsTask();
        });

        var callReturnedWhilePrefixBlocked = await Task.WhenAny(callReturned.Task, Task.Delay(HangTimeout)) == callReturned.Task;
        var completedWhilePrefixBlocked = waiter.IsCompleted;

        fixture.ReleasePrefix();
        await Task.WhenAll(initialization, waiter);

        await Assert.That(callReturnedWhilePrefixBlocked).IsTrue();
        await Assert.That(completedWhilePrefixBlocked).IsFalse();
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    public async Task Waiting_Caller_Observes_Its_Cancellation_While_InitializeAsync_Runs_Synchronously()
    {
        var fixture = new BlockingPrefixInitializer();
        var initialization = Task.Run(() => ObjectInitializer.InitializeAsync(fixture).AsTask());
        await fixture.PrefixEntered.Task.WaitAsync(HangTimeout);

        using var cancellationTokenSource = new CancellationTokenSource();
        var waiter = Task.Run(() => ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask());
        cancellationTokenSource.Cancel();

        var waiterFinishedWhilePrefixBlocked = await Task.WhenAny(waiter, Task.Delay(HangTimeout)) == waiter;

        fixture.ReleasePrefix();
        await initialization;

        await Assert.That(waiterFinishedWhilePrefixBlocked).IsTrue();
        await Assert.That(async () => await waiter).Throws<OperationCanceledException>();
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    public async Task Cancelling_One_Waiter_Does_Not_Cancel_The_Shared_Initialization()
    {
        var fixture = new GatedInitializer();
        var initialization = ObjectInitializer.InitializeAsync(fixture).AsTask();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancelledWaiter = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();
        cancellationTokenSource.Cancel();

        await Assert.That(async () => await cancelledWaiter).Throws<OperationCanceledException>();
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();

        fixture.Complete();
        await initialization.WaitAsync(HangTimeout);
        await ObjectInitializer.InitializeAsync(fixture);

        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    public async Task Waiters_Do_Not_Resume_Inline_On_The_Thread_That_Completes_Initialization()
    {
        var fixture = new GatedInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();
        var initialization = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();

        var completingThreadId = 0;
        var completeReturned = false;

        // Registers its continuation before the initialization is completed below.
        var resumedInline = ResumedInlineAsync(ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token));

        await Task.Run(() =>
        {
            Volatile.Write(ref completingThreadId, Environment.CurrentManagedThreadId);
            fixture.Complete();
            Volatile.Write(ref completeReturned, true);
        });

        await initialization.WaitAsync(HangTimeout);
        await Assert.That(await resumedInline.WaitAsync(HangTimeout)).IsFalse();

        async Task<bool> ResumedInlineAsync(ValueTask pending)
        {
            await pending.ConfigureAwait(false);
            return !Volatile.Read(ref completeReturned)
                   && Environment.CurrentManagedThreadId == Volatile.Read(ref completingThreadId);
        }
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
    [Arguments(true)]
    [Arguments(false)]
    public async Task Failure_Is_Cached_And_Rethrown_To_Every_Caller(bool throwSynchronously)
    {
        var fixture = new ThrowingInitializer(new InvalidOperationException("initialization failed"), throwSynchronously);

        var first = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();
        var second = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();

        await Assert.That(first).IsSameReferenceAs(fixture.Exception);
        await Assert.That(second).IsSameReferenceAs(fixture.Exception);
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();
    }

    [Test]
    public async Task OperationCanceledException_From_InitializeAsync_Reaches_Callers_Unchanged()
    {
        var fixture = new ThrowingInitializer(new OperationCanceledException("initializer gave up"), throwSynchronously: false);

        var first = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<OperationCanceledException>();
        var second = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<OperationCanceledException>();

        await Assert.That(first).IsSameReferenceAs(fixture.Exception);
        await Assert.That(second).IsSameReferenceAs(fixture.Exception);
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();
    }

    /// <summary>
    /// Blocks inside the synchronous part of InitializeAsync (before its first await), like
    /// sync-over-async code in a third-party constructor would.
    /// </summary>
    private sealed class BlockingPrefixInitializer : IAsyncInitializer
    {
        private readonly ManualResetEventSlim _prefixGate = new();
        private int _initializeCount;

        public TaskCompletionSource<bool> PrefixEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public void ReleasePrefix() => _prefixGate.Set();

        public async Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            PrefixEntered.SetResult(true);
            _prefixGate.Wait(PrefixGateTimeout);
            await Task.Yield();
        }
    }

    /// <summary>
    /// Suspends until <see cref="Complete"/> is called; the rest of InitializeAsync then runs
    /// inline on the calling thread.
    /// </summary>
    private sealed class GatedInitializer : IAsyncInitializer
    {
        private readonly TaskCompletionSource<bool> _gate = new();
        private int _initializeCount;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public void Complete() => _gate.SetResult(true);

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

        public Exception Exception { get; } = exception;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            return throwSynchronously ? throw Exception : ThrowAfterYieldAsync();
        }

        private async Task ThrowAfterYieldAsync()
        {
            await Task.Yield();
            throw Exception;
        }
    }
}
