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
    // traceId → testNodeUids (uses ConcurrentDictionary as a set to prevent duplicates)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> TraceToTests =
        new(StringComparer.OrdinalIgnoreCase);

    // testNodeUid → traceIds (uses ConcurrentDictionary as a set to prevent duplicates)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> TestToTraces =
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
        TraceToTests.GetOrAdd(traceId, static _ => new(StringComparer.OrdinalIgnoreCase)).TryAdd(testNodeUid, 0);
        TestToTraces.GetOrAdd(testNodeUid, static _ => new(StringComparer.OrdinalIgnoreCase)).TryAdd(traceId, 0);
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
    /// source trace, or when both trace IDs are the same and the source trace is already
    /// registered; otherwise, <c>false</c>.
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

        foreach (var testNodeUid in testNodeUids)
        {
            Register(derivedTraceId, testNodeUid.Key);
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
        return TestToTraces.TryGetValue(testNodeUid, out var set)
            ? set.Keys.ToArray()
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
