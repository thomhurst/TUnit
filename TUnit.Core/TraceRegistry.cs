#if NET
using System.Collections.Concurrent;

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
