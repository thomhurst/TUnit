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
