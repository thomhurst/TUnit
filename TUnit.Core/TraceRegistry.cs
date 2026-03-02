#if NET
using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Provides cross-project communication between TUnit.Core (where tests run)
/// and TUnit.Engine (where activities are collected) for distributed trace correlation.
/// </summary>
public static class TraceRegistry
{
    // traceId → testNodeUids (uses ConcurrentDictionary as a set to prevent duplicates)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> TraceToTests =
        new(StringComparer.OrdinalIgnoreCase);

    // testNodeUid → traceIds (uses ConcurrentDictionary as a set to prevent duplicates)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> TestToTraces =
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
    /// Returns <c>true</c> if the given trace ID has been registered by any test.
    /// Used by ActivityCollector's sampling callback.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool IsRegistered(string traceId)
    {
        return TraceToTests.ContainsKey(traceId);
    }

    /// <summary>
    /// Gets all trace IDs registered for the given test node UID.
    /// Used by HtmlReporter to populate additional trace IDs on test results.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string[] GetTraceIds(string testNodeUid)
    {
        return TestToTraces.TryGetValue(testNodeUid, out var set)
            ? set.Keys.ToArray()
            : [];
    }
}
#endif
