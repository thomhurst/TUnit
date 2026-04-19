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
}
