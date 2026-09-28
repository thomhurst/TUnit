#if NET
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Core;

/// <summary>
/// Provides cross-project communication between TUnit.Core (where tests run)
/// and TUnit.Engine (where activities are collected) for distributed trace correlation.
/// Accessible to TUnit.Engine via InternalsVisibleTo.
/// </summary>
internal static class TraceRegistry
{
    // traceId → testNodeUids and testNodeUid → traceIds. Each value is a duplicate-free set
    // stored as either an immutable string[] replaced copy-on-write (small sets) or a
    // ConcurrentDictionary<string, byte> (large sets). Every test registers its own trace, so
    // both sides almost always hold a single element; a ConcurrentDictionary per key (bucket
    // and lock arrays) cost ~2KB per test for the same information. A set is promoted to a
    // dictionary once it outgrows SmallSetCapacity, so a trace shared by many tests (e.g. a
    // fixture trace passed to TestContext.RegisterTrace) doesn't degrade to O(N^2) copying.
    private const int SmallSetCapacity = 8;

    private static readonly ConcurrentDictionary<string, object> TraceToTests =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, object> TestToTraces =
        new(StringComparer.OrdinalIgnoreCase);

    // traceId → TestContext.Id (GUID) for cross-process OTLP correlation.
    // Allows the OTLP receiver to resolve traceId → TestContext.GetById(contextId).
    private static readonly ConcurrentDictionary<string, string> TraceToContextId =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a trace ID as associated with a test node UID.
    /// Called by <see cref="TestContext.RegisterTrace"/>.
    /// </summary>
    internal static void Register(string traceId, string testNodeUid)
    {
        AddToSet(TraceToTests, traceId, testNodeUid);
        AddToSet(TestToTraces, testNodeUid, traceId);
    }

    private static void AddToSet(ConcurrentDictionary<string, object> map, string key, string value)
    {
        while (true)
        {
            if (!map.TryGetValue(key, out var existing))
            {
                if (map.TryAdd(key, new[] { value }))
                {
                    return;
                }

                continue;
            }

            if (existing is ConcurrentDictionary<string, byte> large)
            {
                // Promotion is one-way, so once a key holds a dictionary it is mutated in place.
                large.TryAdd(value, 0);
                return;
            }

            var small = (string[])existing;
            foreach (var item in small)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            object updated;
            if (small.Length < SmallSetCapacity)
            {
                var array = new string[small.Length + 1];
                small.CopyTo(array, 0);
                array[small.Length] = value;
                updated = array;
            }
            else
            {
                var promoted = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in small)
                {
                    promoted.TryAdd(item, 0);
                }

                promoted.TryAdd(value, 0);
                updated = promoted;
            }

            // Compares against the instance read above (reference equality), so a concurrent
            // add or promotion makes this fail and retry against the newer value.
            if (map.TryUpdate(key, updated, existing))
            {
                return;
            }
        }
    }

    private static string[] ToArray(object set)
    {
        // Always a fresh array: the stored small arrays are shared state and must not escape.
        return set is string[] small
            ? (string[])small.Clone()
            : [.. ((ConcurrentDictionary<string, byte>)set).Keys];
    }

    /// <summary>
    /// Registers a trace ID with both its test node UID and the TestContext.Id (GUID).
    /// Called by TestExecutor when the test Activity is created, enabling the OTLP
    /// receiver to map incoming telemetry directly to a <see cref="TestContext"/>.
    /// </summary>
    internal static void Register(string traceId, string testNodeUid, string contextId)
    {
        Register(traceId, testNodeUid);
        TraceToContextId[traceId] = contextId;
    }

    /// <summary>
    /// Returns <c>true</c> if the given trace ID has been registered by any test.
    /// Used by ActivityCollector's sampling callback.
    /// </summary>
    internal static bool IsRegistered(string traceId)
    {
        return TraceToTests.ContainsKey(traceId);
    }

    /// <summary>
    /// Gets the <see cref="TestContext.Id"/> (GUID) associated with the given trace ID,
    /// or <c>null</c> if the trace ID is not registered with a context.
    /// Used by the OTLP receiver to route logs to the correct test output.
    /// </summary>
    internal static string? GetContextId(string traceId)
    {
        return TraceToContextId.GetValueOrDefault(traceId);
    }

    /// <summary>
    /// Associates <paramref name="derivedTraceId"/> with the same test(s) as
    /// <paramref name="sourceTraceId"/>. Useful for messaging/queue consumers that start
    /// a new trace but keep a causal link to the original test trace via OTEL span links.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the derived trace was associated with at least one test from the
    /// source trace (span correlation), or when both trace IDs are the same and the source
    /// trace is already registered; otherwise, <c>false</c>.
    /// <para>
    /// A <c>true</c> result does NOT guarantee log routing — if the source trace has no
    /// context-id mapping (only added by the 3-arg <see cref="Register(string,string,string)"/>),
    /// span correlation succeeds but log records for the derived trace fall through
    /// <see cref="GetContextId"/> and are dropped. The case is logged via
    /// <see cref="Trace.WriteLine"/>.
    /// </para>
    /// </returns>
    internal static bool TryRegisterDerivedTrace(string derivedTraceId, string sourceTraceId)
    {
        // Fast path: if both IDs are the same we only need to report whether the source
        // trace is already registered — no dictionary updates required.
        if (string.Equals(derivedTraceId, sourceTraceId, StringComparison.OrdinalIgnoreCase))
        {
            return IsRegistered(sourceTraceId);
        }

        if (!TraceToTests.TryGetValue(sourceTraceId, out var testNodeUids))
        {
            return false;
        }

        if (testNodeUids is string[] small)
        {
            foreach (var testNodeUid in small)
            {
                Register(derivedTraceId, testNodeUid);
            }
        }
        else
        {
            foreach (var entry in (ConcurrentDictionary<string, byte>)testNodeUids)
            {
                Register(derivedTraceId, entry.Key);
            }
        }

        if (TraceToContextId.TryGetValue(sourceTraceId, out var contextId))
        {
            TraceToContextId.TryAdd(derivedTraceId, contextId);
        }
        else
        {
            // Source trace had test associations but no context-id mapping. The derived
            // trace's TraceToTests entry will work for span correlation, but log routing
            // through GetContextId will return null and ProcessLogs will silently drop
            // records. Surface that here so a missing log line in the report points at a
            // concrete cause instead of "nothing happened".
            Trace.WriteLine($"[TUnit.Core] TraceRegistry.TryRegisterDerivedTrace: source trace {sourceTraceId} has no context-id mapping; logs for derived trace {derivedTraceId} will not be routed to a test.");
        }

        return true;
    }

    /// <summary>
    /// Gets all trace IDs registered for the given test node UID.
    /// Used by HtmlReporter to populate additional trace IDs on test results.
    /// </summary>
    internal static string[] GetTraceIds(string testNodeUid)
    {
        // Read once per test when the report is built. Copying keeps callers from mutating
        // registry state; a one- or two-element copy is far cheaper than the per-key
        // dictionaries this storage replaced.
        return TestToTraces.TryGetValue(testNodeUid, out var set)
            ? ToArray(set)
            : [];
    }

    /// <summary>
    /// Clears all registered trace associations. Called at the end of a test run.
    /// </summary>
    internal static void Clear()
    {
        TraceToTests.Clear();
        TestToTraces.Clear();
        TraceToContextId.Clear();
    }
}
#endif
