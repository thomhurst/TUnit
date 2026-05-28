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
    [Timeout(600_000)]
    [Retry(3)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_ConcurrentRpcsOnSameSession_AllReceiveFullResults(string framework, CancellationToken cancellationToken)
    {
        // Regression for https://github.com/thomhurst/TUnit/issues/6001 — concurrent testing/runTests
        // RPCs on a single MTP server-mode session were producing zero results for some calls because
        // per-session state (cancellation token, initializer handlers) was mutated on every RPC.
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);

        var basicTests = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".BasicTests."))
            .ToArray();

        await Assert.That(basicTests).Count().IsGreaterThan(0);

        const int concurrentRpcs = 4;
        const int iterations = 3;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var runTasks = Enumerable.Range(0, concurrentRpcs)
                .Select(_ => session.RunAsync(basicTests, cancellationToken))
                .ToArray();

            var allUpdates = await Task.WhenAll(runTasks);

            for (var rpc = 0; rpc < allUpdates.Length; rpc++)
            {
                var executed = allUpdates[rpc]
                    .Where(x => x.Node.ExecutionState is not null and not "in-progress")
                    .ToArray();

                await Assert.That(executed.Length)
                    .IsEqualTo(basicTests.Length)
                    .Because($"Iteration {iteration}, RPC #{rpc} expected {basicTests.Length} results but received {executed.Length}");
            }
        }
    }

    // Confirms the upstream Microsoft.Testing.Platform behaviour reported in issue #6001
    // (finding 3): sending a non-Guid runId silently coerces to Guid.Empty rather than
    // returning a protocol error. Skipped because the fix lives in microsoft/testfx — keep
    // this as a canonical repro to attach when filing upstream, and to unskip once MTP
    // changes the behaviour.
    [Test, Skip("Confirms MTP-side coercion of malformed runId to Guid.Empty — pending upstream fix in microsoft/testfx (linked from issue #6001).")]
    [Timeout(120_000)]
    [MethodDataSource(nameof(Frameworks))]
    public async Task RunTests_WithMalformedRunId_ServerAttributesUpdatesToGuidEmpty(string framework, CancellationToken cancellationToken)
    {
        await using var session = await TestHostSession.StartAsync(framework, cancellationToken);

        var discovered = await session.DiscoverAsync(cancellationToken);
        var someTest = discovered
            .Select(x => x.Node)
            .Where(node => node.Uid.Contains(".BasicTests."))
            .Take(1)
            .ToArray();
        await Assert.That(someTest).Count().IsGreaterThan(0);

        var emptyRunIdUpdates = new List<TestNodeUpdate>();
        var listener = session.Client.RegisterTestUpdatesListener(Guid.Empty, batch =>
        {
            lock (emptyRunIdUpdates) emptyRunIdUpdates.AddRange(batch);
            return Task.CompletedTask;
        });

        await session.Client.JsonRpcClient.InvokeWithParameterObjectAsync(
            "testing/runTests",
            new
            {
                runId = "not-a-guid",
                tests = someTest
            },
            cancellationToken: cancellationToken);

        await listener.WaitCompletionAsync();

        await Assert.That(emptyRunIdUpdates)
            .Count()
            .IsGreaterThan(0)
            .Because("If MTP rejected the malformed runId as a protocol error, we would never receive notifications attributed to Guid.Empty. If we do, MTP silently coerced.");
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
}
