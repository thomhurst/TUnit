namespace TUnit.Assertions.Tests;

public class ConcurrentAssertMultipleTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConcurrentFailuresAreAllRetained(bool nestedScopes)
    {
        const int workers = 8;
        const int failuresPerWorker = 2_000;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                var tasks = Enumerable.Range(0, workers).Select(worker => Task.Run(async () =>
                {
                    await start.Task;
                    using var nested = nestedScopes ? Assert.Multiple() : null;
                    for (var i = 0; i < failuresPerWorker; i++)
                    {
                        Assert.Fail($"Worker {worker}, failure {i}");
                    }
                })).ToArray();

                start.SetResult();
                await Task.WhenAll(tasks);
            }
        }).Throws<AssertionException>();

        var failures = ((AggregateException)exception!.InnerException!).InnerExceptions;
        await Assert.That(failures.Count).IsEqualTo(workers * failuresPerWorker);
        await Assert.That(failures.Select(failure => failure.Message).Distinct().Count())
            .IsEqualTo(workers * failuresPerWorker);
    }

    [Test]
    public async Task PassingOrChainDoesNotConsumeConcurrentFailure()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                async Task RunChainAsync()
                {
                    await Assert.That(async () =>
                    {
                        started.SetResult();
                        await release.Task;
                        return 1;
                    }).IsEqualTo(1).Or.IsEqualTo(2);
                }

                var chain = RunChainAsync();

                await started.Task;
                Assert.Fail("Independent failure");
                release.SetResult();
                await chain;
            }
        }).Throws<AssertionException>();

        await Assert.That(exception!.Message).IsEqualTo("Independent failure");
    }

    [Test]
    public async Task AndChainStillChecksSecondAssertionAfterConcurrentFailure()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                async Task RunChainAsync()
                {
                    await Assert.That(async () =>
                    {
                        started.SetResult();
                        await release.Task;
                        return 1;
                    }).IsEqualTo(1).And.IsEqualTo(2);
                }

                var chain = RunChainAsync();

                await started.Task;
                Assert.Fail("Independent failure");
                release.SetResult();
                await chain;
            }
        }).Throws<AssertionException>();

        var failures = ((AggregateException)exception!.InnerException!).InnerExceptions;
        await Assert.That(failures.Count).IsEqualTo(2);
        await Assert.That(failures[0].Message).IsEqualTo("Independent failure");
        await Assert.That(failures[1].Message).Contains("and to be 2");
    }

    [Test]
    public async Task SuccessfulPreWorkDoesNotSkipItemAssertionAfterConcurrentFailure()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                async Task RunChainAsync()
                {
                    await Assert.That(async () =>
                    {
                        started.SetResult();
                        await release.Task;
                        return (IEnumerable<int>)[1];
                    }).HasSingleItem().Item.IsEqualTo(2);
                }

                var chain = RunChainAsync();
                await started.Task;
                Assert.Fail("Independent failure");
                release.SetResult();
                await chain;
            }
        }).Throws<AssertionException>();

        var failures = ((AggregateException)exception!.InnerException!).InnerExceptions;
        await Assert.That(failures.Count).IsEqualTo(2);
        await Assert.That(failures[0].Message).IsEqualTo("Independent failure");
        await Assert.That(failures[1].Message).Contains("Expected to be 2");
    }
}
