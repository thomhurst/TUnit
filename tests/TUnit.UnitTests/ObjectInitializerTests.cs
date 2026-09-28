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
    public async Task Waiting_Caller_Observes_Its_Cancellation_While_InitializeAsync_Runs_Synchronously()
    {
        using var fixture = new BlockingPrefixInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();
        var initialization = Task.Run(() => ObjectInitializer.InitializeAsync(fixture).AsTask());
        Task waiter = Task.CompletedTask;
        bool waiterFinishedWhilePrefixBlocked;

        try
        {
            await fixture.PrefixEntered.Task.WaitAsync(HangTimeout);

            waiter = Task.Run(() => ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask());
            cancellationTokenSource.Cancel();

            waiterFinishedWhilePrefixBlocked = await Task.WhenAny(waiter, Task.Delay(HangTimeout)) == waiter;
        }
        finally
        {
            fixture.ReleasePrefix();
        }

        await initialization.WaitAsync(HangTimeout);

        await Assert.That(waiterFinishedWhilePrefixBlocked).IsTrue();
        await Assert.That(async () => await waiter).Throws<OperationCanceledException>();
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
    }

    [Test]
    public async Task Cancelling_A_Waiting_Caller_Does_Not_Cancel_The_Shared_Initialization()
    {
        var fixture = new GatedInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();
        var initializingCaller = ObjectInitializer.InitializeAsync(fixture).AsTask();
        var cancelledWaiter = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();

        cancellationTokenSource.Cancel();

        await Assert.That(async () => await cancelledWaiter).Throws<OperationCanceledException>();
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
    public async Task Cancelling_The_Initializing_Caller_Does_Not_Cancel_The_Shared_Initialization(bool initializationFails)
    {
        var fixture = new GatedInitializer();
        var failure = new InvalidOperationException("initialization failed");
        using var cancellationTokenSource = new CancellationTokenSource();
        var initializingCaller = ObjectInitializer.InitializeAsync(fixture, cancellationTokenSource.Token).AsTask();
        var waiter = ObjectInitializer.InitializeAsync(fixture).AsTask();

        cancellationTokenSource.Cancel();

        await Assert.That(async () => await initializingCaller).Throws<OperationCanceledException>();
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
    [Arguments(true)]
    [Arguments(false)]
    public async Task Waiters_Do_Not_Resume_Inline_On_The_Thread_That_Completes_Initialization(bool cancellableWait)
    {
        var fixture = new GatedInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellableWait ? cancellationTokenSource.Token : CancellationToken.None;
        var initializingCaller = ObjectInitializer.InitializeAsync(fixture, cancellationToken).AsTask();

        var completingThreadId = 0;
        var completeReturned = false;

        // Registers its continuation before the initialization is completed below.
        var waiterResumedInline = ResumedInlineAsync(ObjectInitializer.InitializeAsync(fixture, cancellationToken));

        await Task.Run(() =>
        {
            Volatile.Write(ref completingThreadId, Environment.CurrentManagedThreadId);
            fixture.Complete();
            Volatile.Write(ref completeReturned, true);
        });

        await initializingCaller.WaitAsync(HangTimeout);

        // Precondition: InitializeAsync itself finished inline on the completing thread, so the result
        // is published there too - which is where waiters would run if their continuations were inlined.
        await Assert.That(fixture.ResumedOnThreadId).IsEqualTo(completingThreadId);
        await Assert.That(await waiterResumedInline.WaitAsync(HangTimeout)).IsFalse();

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
    [Arguments(true)]
    [Arguments(false)]
    public async Task OperationCanceledException_From_InitializeAsync_Reaches_Callers_Unchanged(bool throwSynchronously)
    {
        var fixture = new ThrowingInitializer(new OperationCanceledException("initializer gave up"), throwSynchronously);

        var first = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<OperationCanceledException>();
        var second = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<OperationCanceledException>();

        await Assert.That(first).IsSameReferenceAs(fixture.Exception);
        await Assert.That(second).IsSameReferenceAs(fixture.Exception);
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();
    }

    [Test]
    public async Task InitializeAsync_Returning_Null_Fails_Every_Caller_With_The_Same_Exception()
    {
        var fixture = new NullReturningInitializer();

        var first = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();
        var second = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(first!.Message).Contains(nameof(NullReturningInitializer));
        await Assert.That(fixture.InitializeCount).IsEqualTo(1);
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsFalse();
    }

    /// <summary>
    /// Blocks inside the synchronous part of InitializeAsync (before its first await), like
    /// sync-over-async code in a third-party constructor would.
    /// </summary>
    private sealed class BlockingPrefixInitializer : IAsyncInitializer, IDisposable
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
            await Task.Yield();
        }

        public void Dispose() => _prefixGate.Dispose();
    }

    /// <summary>
    /// Suspends until <see cref="Complete"/> or <see cref="Fail"/> is called; the rest of
    /// InitializeAsync then runs inline on the calling thread.
    /// </summary>
    private sealed class GatedInitializer : IAsyncInitializer
    {
        private readonly TaskCompletionSource<bool> _gate = new();
        private int _initializeCount;
        private int _resumedOnThreadId;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public int ResumedOnThreadId => Volatile.Read(ref _resumedOnThreadId);

        public void Complete() => _gate.SetResult(true);

        public void Fail(Exception exception) => _gate.SetException(exception);

        public async Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);

            try
            {
                await _gate.Task;
            }
            finally
            {
                Volatile.Write(ref _resumedOnThreadId, Environment.CurrentManagedThreadId);
            }
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

    private sealed class NullReturningInitializer : IAsyncInitializer
    {
        private int _initializeCount;

        public int InitializeCount => Volatile.Read(ref _initializeCount);

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializeCount);
            return null!;
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
