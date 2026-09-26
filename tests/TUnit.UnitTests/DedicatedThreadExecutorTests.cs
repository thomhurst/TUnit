using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/6885
/// The task returned by a <see cref="DedicatedThreadExecutor"/> must not complete until
/// <c>CleanUp()</c> has returned, whatever the outcome of the test.
/// </summary>
public class DedicatedThreadExecutorTests
{
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task PassingTest_CompletesAfterCleanUp(bool completesSynchronously)
    {
        var executor = new RecordingExecutor();

        await executor.ExecuteTest(TestContext.Current!, completesSynchronously
            ? static () => default
            : static async () => await Task.Delay(50));

        await Assert.That(executor.CleanUpFinished).IsTrue();
    }

    [Test]
    public async Task FailingTest_CompletesAfterCleanUp()
    {
        var executor = new RecordingExecutor();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            try
            {
                await executor.ExecuteTest(TestContext.Current!, static async () =>
                {
                    await Task.Delay(50);
                    throw new InvalidOperationException("test");
                });
            }
            finally
            {
                executor.CleanUpFinishedWhenTestCompleted = executor.CleanUpFinished;
            }
        });

        await Assert.That(exception!.Message).IsEqualTo("test");
        await Assert.That(executor.CleanUpFinishedWhenTestCompleted).IsTrue();
    }

    [Test]
    public async Task CancelledTest_CompletesAfterCleanUp()
    {
        var executor = new RecordingExecutor();

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            try
            {
                await executor.ExecuteTest(TestContext.Current!, static async () =>
                {
                    await Task.Delay(50);
                    throw new OperationCanceledException();
                });
            }
            finally
            {
                executor.CleanUpFinishedWhenTestCompleted = executor.CleanUpFinished;
            }
        });

        await Assert.That(executor.CleanUpFinishedWhenTestCompleted).IsTrue();
    }

    [Test]
    public async Task InitializeFailure_IsReportedAfterCleanUp()
    {
        var executor = new RecordingExecutor { InitializeException = new InvalidOperationException("initialize") };
        var testRan = false;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            try
            {
                await executor.ExecuteTest(TestContext.Current!, () =>
                {
                    testRan = true;
                    return default;
                });
            }
            finally
            {
                executor.CleanUpFinishedWhenTestCompleted = executor.CleanUpFinished;
            }
        });

        await Assert.That(exception!.Message).IsEqualTo("initialize");
        await Assert.That(testRan).IsFalse();
        await Assert.That(executor.CleanUpFinishedWhenTestCompleted).IsTrue();
    }

    [Test]
    public async Task CleanUpFailure_FailsPassingTest()
    {
        var executor = new RecordingExecutor { CleanUpException = new InvalidOperationException("clean up") };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteTest(TestContext.Current!, static () => default).AsTask());

        await Assert.That(exception!.Message).IsEqualTo("clean up");
    }

    [Test]
    public async Task CleanUpFailure_IsReportedAlongsideTestFailure()
    {
        var testException = new InvalidOperationException("test");
        var cleanUpException = new InvalidOperationException("clean up");
        var executor = new RecordingExecutor { CleanUpException = cleanUpException };

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => executor.ExecuteTest(TestContext.Current!, () => throw testException).AsTask());

        await Assert.That(exception!.InnerExceptions.Count).IsEqualTo(2);
        await Assert.That(exception.InnerExceptions[0]).IsSameReferenceAs(testException);
        await Assert.That(exception.InnerExceptions[1]).IsSameReferenceAs(cleanUpException);
    }

    [Test]
    public async Task CleanUpFailure_IsReportedForCancelledTest()
    {
        var cleanUpException = new InvalidOperationException("clean up");
        var executor = new RecordingExecutor { CleanUpException = cleanUpException };

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => executor.ExecuteTest(TestContext.Current!, static async () =>
            {
                await Task.Yield();
                throw new OperationCanceledException();
            }).AsTask());

        await Assert.That(exception!.InnerExceptions.Count).IsEqualTo(2);
        await Assert.That(exception.InnerExceptions[0]).IsTypeOf<TaskCanceledException>();
        await Assert.That(exception.InnerExceptions[1]).IsSameReferenceAs(cleanUpException);
    }

    private sealed class RecordingExecutor : DedicatedThreadExecutor
    {
        private volatile bool _cleanUpFinished;

        public Exception? InitializeException { get; init; }

        public Exception? CleanUpException { get; init; }

        public bool CleanUpFinished => _cleanUpFinished;

        public bool CleanUpFinishedWhenTestCompleted { get; set; }

        protected override void Initialize()
        {
            if (InitializeException is not null)
            {
                throw InitializeException;
            }
        }

        protected override void CleanUp()
        {
            // Long enough that anything waiting on the test would observe CleanUp() still running.
            Thread.Sleep(100);
            _cleanUpFinished = true;

            if (CleanUpException is not null)
            {
                throw CleanUpException;
            }
        }
    }
}
