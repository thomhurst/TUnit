using System.Diagnostics;

namespace TUnit.Assertions.Tests;

[NotInParallel]
public class ExecutionTimeTests
{
    [Test]
    public async Task Completes_Within_Happy()
    {
        var action = () => Thread.Sleep(1);

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy()
    {
        var stopwatch = Stopwatch.StartNew();

        var action = () => Thread.Sleep(10000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        // Well under the 10s action, so the 250ms limit must have cut it short, but loose enough
        // for loaded CI runners where a sub-second budget is flaky (see #7169).
        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Completes_Within_Happy2()
    {
        var action = () => Task.Delay(1);

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy2()
    {
        var stopwatch = Stopwatch.StartNew();

        var action = () => Task.Delay(10000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        // Well under the 10s action, so the 250ms limit must have cut it short, but loose enough
        // for loaded CI runners where a sub-second budget is flaky (see #7169).
        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Completes_Within_Returns_The_Result_Of_A_Func()
    {
        var result = await Assert.That(() => Task.FromResult(42)).CompletesWithin(TimeSpan.FromSeconds(10));

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Completes_Within_Returns_The_Result_Of_A_Task()
    {
        var result = await Assert.That(ReplyAsync()).CompletesWithin(TimeSpan.FromSeconds(10));

        await Assert.That(result).IsEqualTo("reply");

        static async Task<string> ReplyAsync()
        {
            await Task.Delay(1);
            return "reply";
        }
    }

    [Test]
    public async Task Completes_Within_Returns_The_Result_Of_A_Func_Returning_IEnumerable()
    {
        // A function returning Task<IEnumerable<T>> binds to the collection source of Assert.That
        var items = await Assert.That(async () => await GetItemsAsync()).CompletesWithin(TimeSpan.FromSeconds(10));

        await Assert.That(items).IsEquivalentTo([1, 2, 3]);

        var neverCompletes = new TaskCompletionSource<IEnumerable<int>?>().Task;

        await Assert.That(
            async () => await Assert.That(() => neverCompletes).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        static async Task<IEnumerable<int>> GetItemsAsync()
        {
            await Task.Delay(1);
            return [1, 2, 3];
        }
    }

    [Test]
    public async Task Completes_Within_Chains_A_Check_Of_The_Result()
    {
        await Assert.That(() => Task.FromResult(42)).CompletesWithin(TimeSpan.FromSeconds(10)).And.IsEqualTo(42);
    }

    [Test]
    public async Task Completes_Within_Unhappy_With_A_Result()
    {
        var stopwatch = Stopwatch.StartNew();

        var func = async () =>
        {
            await Task.Delay(10000);
            return 1;
        };

        await Assert.That(
            async () => await Assert.That(func).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Completes_Within_Fails_A_Function_That_Blocks_Past_The_Limit()
    {
        Func<Task> blocks = () =>
        {
            Thread.Sleep(1000);
            return Task.CompletedTask;
        };
        Func<Task<int>> blocksWithAResult = () =>
        {
            Thread.Sleep(1000);
            return Task.FromResult(1);
        };

        await Assert.That(
            async () => await Assert.That(blocks).CompletesWithin(TimeSpan.FromMilliseconds(50))
        ).Throws<AssertionException>()
        .WithMessageContaining("but it took too long to complete");

        await Assert.That(
            async () => await Assert.That(blocksWithAResult).CompletesWithin(TimeSpan.FromMilliseconds(50))
        ).Throws<AssertionException>()
        .WithMessageContaining("but it took too long to complete");
    }

    [Test]
    public async Task Completes_Within_Gives_The_Exception_Of_Work_That_Faulted_Past_The_Limit()
    {
        Func<Task> blocksThenFaults = () =>
        {
            Thread.Sleep(1000);
            return Task.FromException(new InvalidOperationException("boom"));
        };
        Func<Task<int>> blocksThenFaultsWithAResult = () =>
        {
            Thread.Sleep(1000);
            return Task.FromException<int>(new InvalidOperationException("boom"));
        };

        var exception = await Assert.That(
            async () => await Assert.That(blocksThenFaults).CompletesWithin(TimeSpan.FromMilliseconds(50))
        ).Throws<AssertionException>()
        .WithMessageContaining("but it took too long to complete");

        await Assert.That(exception!.InnerException).IsTypeOf<InvalidOperationException>();

        var exceptionWithAResult = await Assert.That(
            async () => await Assert.That(blocksThenFaultsWithAResult).CompletesWithin(TimeSpan.FromMilliseconds(50))
        ).Throws<AssertionException>()
        .WithMessageContaining("but it took too long to complete");

        await Assert.That(exceptionWithAResult!.InnerException).IsTypeOf<InvalidOperationException>();
    }

    [Test]
    public async Task Completes_Within_Names_The_Timeout_In_An_Or_Chain()
    {
        var neverCompletes = new TaskCompletionSource<int?>().Task;

        await Assert.That(
            async () => await Assert.That(neverCompletes).CompletesWithin(TimeSpan.FromMilliseconds(50)).Or.IsNull()
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw TUnit.Assertions.Conditions.CompletesWithinTimeoutException");
    }

    [Test]
    [Arguments(0L)]
    [Arguments(1L)]
    [Arguments(5_000L)]
    [Arguments(9_999L)]
    public async Task Completes_Within_A_Limit_Under_A_Millisecond_Passes_Only_Work_That_Is_Already_Complete(long ticks)
    {
        // Task.Delay rounds a limit under a millisecond down to zero
        var limit = TimeSpan.FromTicks(ticks);
        Task completed = Task.FromResult(1);
        var pending = new TaskCompletionSource<int>().Task;

        await Assert.That(Task.CompletedTask).CompletesWithin(limit);
        await Assert.That(completed).CompletesWithin(limit);
        await Assert.That(Task.FromResult(1)).CompletesWithin(limit);
        await Assert.That(() => Task.FromResult(1)).CompletesWithin(limit);

        await Assert.That(
            async () => await Assert.That(pending).CompletesWithin(limit)
        ).Throws<AssertionException>()
        .WithMessageContaining("but it took too long to complete");
    }

    [Test]
    public async Task Completes_Within_Reports_A_Canceled_Action_As_Canceled()
    {
        Action action = () => throw new OperationCanceledException();

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw System.OperationCanceledException");
    }

    [Test]
    public async Task Completes_Within_Reports_A_Canceled_Task_As_Canceled()
    {
        var canceled = Task.FromCanceled(new CancellationToken(canceled: true));

        await Assert.That(
            async () => await Assert.That(() => canceled).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw System.Threading.Tasks.TaskCanceledException");
    }

    [Test]
    public async Task Completes_Within_Reports_A_Canceled_Task_With_A_Result_As_Canceled()
    {
        var canceled = Task.FromCanceled<int>(new CancellationToken(canceled: true));

        await Assert.That(
            async () => await Assert.That(canceled).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw System.Threading.Tasks.TaskCanceledException");
    }

    [Test]
    public async Task Completes_Within_Unhappy_With_A_Task_Result()
    {
        var stopwatch = Stopwatch.StartNew();

        var neverCompletes = new TaskCompletionSource<int>().Task;

        await Assert.That(
            async () => await Assert.That(neverCompletes).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Completes_Within_Reports_A_Faulted_Task_With_A_Result()
    {
        var faulted = Task.FromException<int>(new InvalidOperationException("boom"));
        Func<Task<int>> throwsAtOnce = () => throw new InvalidOperationException("at once");

        await Assert.That(
            async () => await Assert.That(faulted).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw System.InvalidOperationException: boom");

        await Assert.That(
            async () => await Assert.That(throwsAtOnce).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("but threw System.InvalidOperationException: at once");
    }

    [Test]
    public async Task Completes_Within_Returns_A_Null_Result()
    {
        var result = await Assert.That(Task.FromResult<string?>(null)).CompletesWithin(TimeSpan.FromSeconds(10));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Completes_Within_Rejects_A_Negative_Timeout_Before_Starting_The_Work()
    {
        var started = 0;
        Action action = () => started++;
        Func<Task> asyncAction = () =>
        {
            started++;
            return Task.CompletedTask;
        };
        Func<Task<int>> func = () =>
        {
            started++;
            return Task.FromResult(1);
        };

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(-5))
        ).Throws<ArgumentOutOfRangeException>();
        await Assert.That(
            async () => await Assert.That(asyncAction).CompletesWithin(TimeSpan.FromMilliseconds(-5))
        ).Throws<ArgumentOutOfRangeException>();
        await Assert.That(
            async () => await Assert.That(func).CompletesWithin(TimeSpan.FromMilliseconds(-5))
        ).Throws<ArgumentOutOfRangeException>();

        await Assert.That(started).IsEqualTo(0);
    }

    [Test]
    public async Task Completes_Within_Failure_Names_The_Assertion()
    {
        Action action = () => throw new InvalidOperationException("boom");
        var faulted = Task.FromException(new InvalidOperationException("boom"));
        var faultedWithResult = Task.FromException<int>(new InvalidOperationException("boom"));

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("at Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10))");

        await Assert.That(
            async () => await Assert.That(() => faulted).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("at Assert.That(() => faulted).CompletesWithin(TimeSpan.FromSeconds(10))");

        await Assert.That(
            async () => await Assert.That(faultedWithResult).CompletesWithin(TimeSpan.FromSeconds(10))
        ).Throws<AssertionException>()
        .WithMessageContaining("at Assert.That(faultedWithResult).CompletesWithin(TimeSpan.FromSeconds(10))");
    }

    [Test]
    public async Task Completes_Within_Stops_Its_Timer_When_The_Work_Completes_First()
    {
        Func<Task> throwsAtOnce = () => throw new InvalidOperationException();
        Func<Task<int>> throwsAtOnceWithResult = () => throw new InvalidOperationException();
        var before = System.Threading.Timer.ActiveCount;

        for (var i = 0; i < 100; i++)
        {
            await Assert.That(() => { }).CompletesWithin(TimeSpan.FromHours(1));
            await Assert.That(() => Task.CompletedTask).CompletesWithin(TimeSpan.FromHours(1));
            await Assert.That(() => Task.FromResult(i)).CompletesWithin(TimeSpan.FromHours(1));
            await Assert.That(async () => await Assert.That(throwsAtOnce).CompletesWithin(TimeSpan.FromHours(1)))
                .Throws<AssertionException>();
            await Assert.That(async () => await Assert.That(throwsAtOnceWithResult).CompletesWithin(TimeSpan.FromHours(1)))
                .Throws<AssertionException>();
        }

        // Each passing call used to leave its one-hour timer running (300 here), and a call whose
        // work throws at once must not either. The class runs alone ([NotInParallel]): timers of
        // earlier tests can only expire meanwhile, and the margin covers the runner's own.
        await Assert.That(System.Threading.Timer.ActiveCount - before).IsLessThan(50);
    }
}
