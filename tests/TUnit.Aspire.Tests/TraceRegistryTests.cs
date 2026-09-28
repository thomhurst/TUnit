using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class TraceRegistryTests
{
    // Use unique trace IDs per test to avoid cross-test interference
    // (TraceRegistry is a static singleton)

    [Test]
    public async Task Register_And_GetContextId_ReturnsContextId()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var contextId = Guid.NewGuid().ToString();

        TraceRegistry.Register(traceId, "node-1", contextId);

        await Assert.That(TraceRegistry.GetContextId(traceId)).IsEqualTo(contextId);
    }

    [Test]
    public async Task GetContextId_UnknownTraceId_ReturnsNull()
    {
        var result = TraceRegistry.GetContextId("0000000000000000UNKNOWN_TRACE_ID");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task IsRegistered_KnownTraceId_ReturnsTrue()
    {
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "node-2");

        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
    }

    [Test]
    public async Task IsRegistered_UnknownTraceId_ReturnsFalse()
    {
        await Assert.That(TraceRegistry.IsRegistered("FFFFFFFF_NEVER_REGISTERED")).IsFalse();
    }

    [Test]
    public async Task GetTraceIds_ReturnsAllTracesForTest()
    {
        var testNodeUid = $"test-{Guid.NewGuid():N}";
        var traceId1 = Guid.NewGuid().ToString("N");
        var traceId2 = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId1, testNodeUid);
        TraceRegistry.Register(traceId2, testNodeUid);

        var traces = TraceRegistry.GetTraceIds(testNodeUid);

        await Assert.That(traces).Count().IsEqualTo(2);
        await Assert.That(traces).Contains(traceId1);
        await Assert.That(traces).Contains(traceId2);
    }

    [Test]
    public async Task GetTraceIds_UnknownTestNodeUid_ReturnsEmpty()
    {
        var result = TraceRegistry.GetTraceIds("unknown-node-uid-never-registered");

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Register_CaseInsensitive_LookupWorks()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var contextId = Guid.NewGuid().ToString();

        TraceRegistry.Register(traceId.ToLowerInvariant(), "node-ci", contextId);

        await Assert.That(TraceRegistry.GetContextId(traceId.ToUpperInvariant())).IsEqualTo(contextId);
        await Assert.That(TraceRegistry.IsRegistered(traceId.ToUpperInvariant())).IsTrue();
    }

    [Test]
    public async Task Register_DuplicateTraceId_DoesNotThrow()
    {
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "node-dup");
        TraceRegistry.Register(traceId, "node-dup");

        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
    }

    [Test]
    public async Task Register_DuplicateTraceIdDifferingOnlyInCase_IsStoredOnce()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var testNodeUid = $"node-{Guid.NewGuid():N}";

        TraceRegistry.Register(traceId, testNodeUid);
        TraceRegistry.Register(traceId.ToUpperInvariant(), testNodeUid);

        await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).IsEquivalentTo([traceId]);
    }

    [Test]
    public async Task Register_ConcurrentTracesForOneTest_KeepsEveryTrace()
    {
        var testNodeUid = $"node-{Guid.NewGuid():N}";
        var traceIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid().ToString("N")).ToArray();

        Parallel.ForEach(traceIds, traceId => TraceRegistry.Register(traceId, testNodeUid));

        await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).IsEquivalentTo(traceIds);

        foreach (var traceId in traceIds)
        {
            await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
        }
    }

    [Test]
    public async Task Register_ConcurrentTestsForOneTrace_KeepsEveryTest()
    {
        // One shared trace (e.g. a fixture's) registered by many tests at once: exercises the
        // small-array to dictionary promotion under contention.
        var traceId = Guid.NewGuid().ToString("N");
        var testNodeUids = Enumerable.Range(0, 200).Select(_ => $"node-{Guid.NewGuid():N}").ToArray();

        Parallel.ForEach(testNodeUids, testNodeUid => TraceRegistry.Register(traceId, testNodeUid));

        foreach (var testNodeUid in testNodeUids)
        {
            await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).IsEquivalentTo([traceId]);
        }

        // The derived trace copies the source trace's test set, so it proves every test is present.
        var derivedTraceId = Guid.NewGuid().ToString("N");
        await Assert.That(TraceRegistry.TryRegisterDerivedTrace(derivedTraceId, traceId)).IsTrue();

        foreach (var testNodeUid in testNodeUids)
        {
            await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).IsEquivalentTo([traceId, derivedTraceId]);
        }
    }

    [Test]
    public async Task Register_ConcurrentDuplicateRegistrations_StoredOnceInBothDirections()
    {
        var traceIds = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid().ToString("N")).ToArray();
        var testNodeUids = Enumerable.Range(0, 20).Select(_ => $"node-{Guid.NewGuid():N}").ToArray();

        // Every (trace, test) pair registered several times, in mixed case, from racing threads.
        var registrations = (
            from traceId in traceIds
            from testNodeUid in testNodeUids
            from attempt in Enumerable.Range(0, 4)
            select (TraceId: attempt % 2 == 0 ? traceId : traceId.ToUpperInvariant(), TestNodeUid: testNodeUid)
        ).ToArray();

        Parallel.ForEach(registrations, r => TraceRegistry.Register(r.TraceId, r.TestNodeUid));

        foreach (var testNodeUid in testNodeUids)
        {
            await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).Count().IsEqualTo(traceIds.Length);
        }

        // Test → trace direction checked above; derive from each trace to check trace → tests.
        foreach (var traceId in traceIds)
        {
            var probeTraceId = Guid.NewGuid().ToString("N");
            await Assert.That(TraceRegistry.TryRegisterDerivedTrace(probeTraceId, traceId)).IsTrue();

            foreach (var testNodeUid in testNodeUids)
            {
                await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).Contains(probeTraceId);
            }
        }

        foreach (var testNodeUid in testNodeUids)
        {
            await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).Count().IsEqualTo(traceIds.Length * 2);
        }
    }

    [Test]
    public async Task GetTraceIds_ReturnsCopy_MutationDoesNotAffectRegistry()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var testNodeUid = $"node-{Guid.NewGuid():N}";
        TraceRegistry.Register(traceId, testNodeUid);

        var traces = TraceRegistry.GetTraceIds(testNodeUid);
        traces[0] = "tampered";

        await Assert.That(TraceRegistry.GetTraceIds(testNodeUid)).IsEquivalentTo([traceId]);
    }

    [Test]
    public async Task Register_WithContextId_OverwritesPreviousContextId()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var contextId1 = Guid.NewGuid().ToString();
        var contextId2 = Guid.NewGuid().ToString();

        TraceRegistry.Register(traceId, "node-overwrite", contextId1);
        TraceRegistry.Register(traceId, "node-overwrite", contextId2);

        await Assert.That(TraceRegistry.GetContextId(traceId)).IsEqualTo(contextId2);
    }
}
