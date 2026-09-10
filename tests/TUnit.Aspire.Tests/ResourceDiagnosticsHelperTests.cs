using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Pure (no Docker) tests for the resource-diagnostics helpers added in #6343: the bounded log
/// buffer's rolling/cap semantics, and the graceful behavior of the public helpers on a fixture
/// that has not been initialized. The buffer-backed and snapshot-reading paths of a running app are
/// exercised by the Docker-backed integration tests.
/// </summary>
public class ResourceDiagnosticsHelperTests
{
    // --- BoundedLogBuffer: rolling window + cap ---

    [Test]
    public async Task Buffer_ClampsNonPositiveCapacityToOne()
    {
        var buffer = new BoundedLogBuffer(0);
        buffer.Add("a");
        buffer.Add("b");

        await Assert.That(buffer.Capacity).IsEqualTo(1);
        await Assert.That(buffer.Snapshot(-1)).IsEquivalentTo(new[] { "b" });
    }

    [Test]
    public async Task Buffer_EvictsOldestBeyondCapacity()
    {
        var buffer = new BoundedLogBuffer(3);
        foreach (var line in new[] { "1", "2", "3", "4", "5" })
        {
            buffer.Add(line);
        }

        // Only the last 3 survive, oldest first.
        await Assert.That(buffer.Snapshot(-1)).IsEquivalentTo(new[] { "3", "4", "5" });
    }

    [Test]
    public async Task Buffer_SnapshotReturnsMostRecentN()
    {
        var buffer = new BoundedLogBuffer(10);
        foreach (var line in new[] { "1", "2", "3", "4", "5" })
        {
            buffer.Add(line);
        }

        await Assert.That(buffer.Snapshot(2)).IsEquivalentTo(new[] { "4", "5" });
    }

    [Test]
    public async Task Buffer_SnapshotBeyondCount_ReturnsAll()
    {
        var buffer = new BoundedLogBuffer(10);
        buffer.Add("1");
        buffer.Add("2");

        await Assert.That(buffer.Snapshot(50)).IsEquivalentTo(new[] { "1", "2" });
    }

    [Test]
    public async Task Buffer_SnapshotZero_ReturnsEmpty()
    {
        var buffer = new BoundedLogBuffer(10);
        buffer.Add("1");

        await Assert.That(buffer.Snapshot(0)).IsEmpty();
    }

    [Test]
    public async Task Buffer_EmptySnapshot_IsEmpty()
        => await Assert.That(new BoundedLogBuffer(5).Snapshot(-1)).IsEmpty();

    // --- Public helpers on an un-initialized fixture: no throw, graceful defaults ---

    private static AspireFixture<Projects.TUnit_Aspire_Tests_AppHost> NewFixture() => new();

    [Test]
    public async Task GetResourceLogsAsync_NotInitialized_ReturnsEmpty()
        => await Assert.That(await NewFixture().GetResourceLogsAsync("chat")).IsEmpty();

    [Test]
    public async Task GetResourceSnapshot_NotInitialized_ReturnsNull()
        => await Assert.That(NewFixture().GetResourceSnapshot("chat")).IsNull();

    [Test]
    public async Task DumpResourceDiagnosticsAsync_NotInitialized_ReturnsNotInitializedMessage()
        => await Assert.That(await NewFixture().DumpResourceDiagnosticsAsync())
            .IsEqualTo("Aspire application not initialized.");
}
