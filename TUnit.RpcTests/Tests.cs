using TUnit.RpcTests.Models;

namespace TUnit.RpcTests;

[NotInParallel(nameof(Tests))]
public class Tests
{
    public static IEnumerable<Func<string>> Frameworks()
    {
        yield return () => "net8.0";
        yield return () => "net10.0";
    }

    [Test]
    [Timeout(300_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task Discovery_ReturnsFullTestCatalogue(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var updates = await session.DiscoverAsync(cancellationToken);

        var discovered = updates.Where(x => x.Node.ExecutionState is "discovered").ToArray();
        await Assert.That(discovered).Count().IsGreaterThanOrEqualTo(4000);
    }

    [Test]
    [Timeout(300_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_WithUidFilter_ExecutesOnlySelectedTests(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);

        var basicTests = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".BasicTests."))
            .ToArray();

        await Assert.That(basicTests).Count().IsGreaterThan(0);

        var runUpdates = await session.RunAsync(basicTests, cancellationToken);

        var executed = runUpdates.Where(x => x.Node.ExecutionState is not null and not "in-progress").ToArray();
        var passed = executed.Count(x => x.Node.ExecutionState == "passed");

        using (Assert.Multiple())
        {
            await Assert.That(executed).Count().IsEqualTo(basicTests.Length);
            await Assert.That(passed).IsEqualTo(basicTests.Length);
        }
    }

    [Test]
    [Timeout(300_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_WithSkippedTest_ReportsSkippedState(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);

        var skipTest = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".SimpleSkipTest."))
            .ToArray();

        await Assert.That(skipTest).Count().IsGreaterThan(0);

        var runUpdates = await session.RunAsync(skipTest, cancellationToken);

        var skipped = runUpdates
            .Where(x => x.Node.ExecutionState == "skipped")
            .ToArray();

        await Assert.That(skipped).Count().IsGreaterThan(0);
    }

    [Test]
    [Timeout(300_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_MultipleSequentialSessions_ProducesConsistentResults(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);

        var basicTests = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".BasicTests."))
            .ToArray();

        await Assert.That(basicTests).Count().IsGreaterThan(0);

        // Run the same tests 3 times sequentially against the same server process.
        // Each run should produce the same number of results — verifying that shared
        // static state (Sources, GlobalContext, hooks) is not drained or corrupted.
        for (var i = 0; i < 3; i++)
        {
            var runUpdates = await session.RunAsync(basicTests, cancellationToken);

            var executed = runUpdates.Where(x => x.Node.ExecutionState is not null and not "in-progress").ToArray();
            var passed = executed.Count(x => x.Node.ExecutionState == "passed");

            using (Assert.Multiple())
            {
                await Assert.That(executed).Count().IsEqualTo(basicTests.Length)
                    .Because($"Sequential run {i + 1} of 3 should execute all {basicTests.Length} tests");
                await Assert.That(passed).IsEqualTo(basicTests.Length)
                    .Because($"Sequential run {i + 1} of 3 should pass all tests");
            }
        }
    }

    [Test]
    [Timeout(300_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_ConcurrentSessions_ProducesResults(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);

        var basicTests = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".BasicTests."))
            .ToArray();

        await Assert.That(basicTests).Count().IsGreaterThan(0);

        // Fire 3 concurrent runTests RPCs to the same server process.
        // With the serialization fix, all should complete with correct results
        // (they queue up and execute sequentially under the lock).
        var tasks = Enumerable.Range(0, 3)
            .Select(_ => session.RunAsync(basicTests, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < results.Length; i++)
        {
            var executed = results[i].Where(x => x.Node.ExecutionState is not null and not "in-progress").ToArray();
            var passed = executed.Count(x => x.Node.ExecutionState == "passed");

            using (Assert.Multiple())
            {
                await Assert.That(executed).Count().IsEqualTo(basicTests.Length)
                    .Because($"Concurrent run {i + 1} of 3 should execute all {basicTests.Length} tests");
                await Assert.That(passed).IsEqualTo(basicTests.Length)
                    .Because($"Concurrent run {i + 1} of 3 should pass all tests");
            }
        }
    }
}
