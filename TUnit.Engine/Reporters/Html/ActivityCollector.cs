#if NET
using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Engine.Reporters.Html;

internal sealed class ActivityCollector : IDisposable
{
    // Cap external (non-TUnit) spans per test to keep the report manageable.
    // TUnit's own spans are always captured regardless of caps.
    // Soft cap — intentionally racy for performance; may be slightly exceeded under high concurrency.
    private const int MaxExternalSpansPerTest = 100;
    // Fallback cap applied per trace when the test case association cannot be determined
    // (e.g. broken Activity.Parent chains from async connection pooling).
    private const int MaxExternalSpansPerTrace = 100;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<SpanData>> _spansByTrace = new();
    // Track external span count per test case (keyed by test case span ID)
    private readonly ConcurrentDictionary<string, int> _externalSpanCountsByTest = new();
    // Fallback: per-trace cap for external spans whose parent chain is broken
    // (e.g. Npgsql async pooling where Activity.Parent is null but traceId is correct)
    private readonly ConcurrentDictionary<string, int> _externalSpanCountsByTrace = new();
    // Known test case span IDs, populated at activity start time so they're available
    // before child spans stop (children stop before parents in Activity ordering).
    private readonly ConcurrentDictionary<string, byte> _testCaseSpanIds = new();
    // Fast-path cache of trace IDs that should be collected. Subsumes TraceRegistry lookups
    // so that subsequent activities on the same trace avoid cross-class dictionary checks.
    private readonly ConcurrentDictionary<string, byte> _knownTraceIds = new(StringComparer.OrdinalIgnoreCase);
    private ActivityListener? _listener;

    public void Start()
    {
        // Listen to ALL sources so we can capture child spans from HttpClient, ASP.NET Core,
        // EF Core, etc. The Sample callback uses smart filtering to avoid overhead: only spans
        // belonging to known test traces are fully recorded; everything else gets PropagationData
        // (near-zero cost — enables context flow without timing/tags).
        _listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleActivity,
            SampleUsingParentId = SampleActivityUsingParentId,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };

        ActivitySource.AddActivityListener(_listener);
    }

    private ActivitySamplingResult SampleActivity(ref ActivityCreationOptions<ActivityContext> options)
    {
        var sourceName = options.Source.Name;

        // TUnit/Microsoft.Testing sources: always record, register trace
        if (IsTUnitSource(sourceName))
        {
            if (options.Parent.TraceId != default)
            {
                _knownTraceIds.TryAdd(options.Parent.TraceId.ToString(), 0);
            }

            return ActivitySamplingResult.AllDataAndRecorded;
        }

        // No parent trace → nothing to correlate with
        if (options.Parent.TraceId == default)
        {
            return ActivitySamplingResult.PropagationData;
        }

        var parentTraceId = options.Parent.TraceId.ToString();

        // Parent trace is known (child of a TUnit activity, e.g. HttpClient)
        if (_knownTraceIds.ContainsKey(parentTraceId))
        {
            return ActivitySamplingResult.AllDataAndRecorded;
        }

        // Trace registered via TestContext.RegisterTrace
        if (TraceRegistry.IsRegistered(parentTraceId))
        {
            _knownTraceIds.TryAdd(parentTraceId, 0);
            return ActivitySamplingResult.AllDataAndRecorded;
        }

        // Everything else: create the Activity for context propagation but no timing/tags
        return ActivitySamplingResult.PropagationData;
    }

    private ActivitySamplingResult SampleActivityUsingParentId(ref ActivityCreationOptions<string> options)
    {
        if (IsTUnitSource(options.Source.Name))
        {
            return ActivitySamplingResult.AllDataAndRecorded;
        }

        // Try to extract the trace ID from W3C format: "00-{32-hex-traceId}-{16-hex-spanId}-{2-hex-flags}"
        var parentId = options.Parent;
        if (parentId is { Length: >= 35 } && parentId[2] == '-')
        {
            var traceIdStr = parentId.Substring(3, 32);
            if (_knownTraceIds.ContainsKey(traceIdStr) || TraceRegistry.IsRegistered(traceIdStr))
            {
                _knownTraceIds.TryAdd(traceIdStr, 0);
                return ActivitySamplingResult.AllDataAndRecorded;
            }
        }

        return ActivitySamplingResult.PropagationData;
    }

    public void Stop()
    {
        _listener?.Dispose();
        _listener = null;
    }

    public SpanData[] GetAllSpans()
    {
        return _spansByTrace.Values.SelectMany(q => q).ToArray();
    }

    /// <summary>
    /// Builds a lookup from TestNode UID to (TraceId, SpanId) by finding the root
    /// "test case" span for each test via the <c>tunit.test.node_uid</c> activity tag.
    /// </summary>
    public Dictionary<string, (string TraceId, string SpanId)> GetTestSpanLookup()
    {
        var lookup = new Dictionary<string, (string, string)>();

        foreach (var kvp in _spansByTrace)
        {
            foreach (var span in kvp.Value)
            {
                if (span.Tags is null)
                {
                    continue;
                }

                foreach (var tag in span.Tags)
                {
                    if (tag.Key == "tunit.test.node_uid" && !string.IsNullOrEmpty(tag.Value))
                    {
                        lookup[tag.Value] = (span.TraceId, span.SpanId);
                        break;
                    }
                }
            }
        }

        return lookup;
    }

    private void OnActivityStarted(Activity activity)
    {
        // Register test case span IDs early so they're available for child span lookups.
        // Children stop before parents in Activity ordering, so we need this pre-registered.
        if (IsTUnitSource(activity.Source.Name) &&
            activity.GetTagItem("tunit.test.node_uid") is not null)
        {
            _testCaseSpanIds.TryAdd(activity.SpanId.ToString(), 0);
        }
    }

    private string? FindTestCaseAncestor(Activity activity)
    {
        // First: walk in-memory parent chain (works when parent Activity is alive)
        var current = activity.Parent;
        while (current is not null)
        {
            if (IsTUnitSource(current.Source.Name) &&
                current.GetTagItem("tunit.test.node_uid") is not null)
            {
                return current.SpanId.ToString();
            }

            current = current.Parent;
        }

        // Fallback: check if the direct ParentSpanId is a known test case span.
        // Note: only one level — deeper broken chains fall through to the per-trace cap.
        // This handles Npgsql async pooling where the direct parent reference is broken
        // but W3C ParentSpanId is still correct.
        if (activity.ParentSpanId != default)
        {
            var parentSpanId = activity.ParentSpanId.ToString();
            if (_testCaseSpanIds.ContainsKey(parentSpanId))
            {
                return parentSpanId;
            }
        }

        return null;
    }

    private static bool IsTUnitSource(string sourceName) =>
        sourceName.StartsWith("TUnit", StringComparison.Ordinal) ||
        sourceName.StartsWith("Microsoft.Testing", StringComparison.Ordinal);

    private static string EnrichSpanName(Activity activity)
    {
        var displayName = activity.DisplayName;

        // Look up the semantic name tag to produce a more descriptive label
        var tagKey = displayName switch
        {
            "test case" => "test.case.name",
            "test suite" => "test.suite.name",
            "test assembly" => "tunit.assembly.name",
            _ => null
        };

        if (tagKey is not null)
        {
            var value = activity.GetTagItem(tagKey)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return displayName;
    }

    private void OnActivityStopped(Activity activity)
    {
        var traceId = activity.TraceId.ToString();
        var isTUnit = IsTUnitSource(activity.Source.Name);

        // TUnit activities always register their own trace ID. This catches root activities
        // (e.g. "test session") whose TraceId is assigned by the runtime after sampling,
        // so it couldn't be registered in SampleActivity where only the parent TraceId is known.
        if (isTUnit)
        {
            _knownTraceIds.TryAdd(traceId, 0);
        }
        else if (!_knownTraceIds.ContainsKey(traceId))
        {
            return;
        }

        // Cap external spans per test to keep the report size manageable.
        // TUnit's own spans are always captured — they're essential for the report.
        if (!isTUnit)
        {
            var testSpanId = FindTestCaseAncestor(activity);
            if (testSpanId is not null)
            {
                var count = _externalSpanCountsByTest.AddOrUpdate(testSpanId, 1, (_, c) => c + 1);
                if (count > MaxExternalSpansPerTest)
                {
                    return;
                }
            }
            else
            {
                // Fallback cap by trace ID to prevent unbounded growth for spans
                // with broken parent chains (e.g., Npgsql async connection pooling).
                var count = _externalSpanCountsByTrace.AddOrUpdate(traceId, 1, (_, c) => c + 1);
                if (count > MaxExternalSpansPerTrace)
                {
                    return;
                }
            }
        }

        var queue = _spansByTrace.GetOrAdd(traceId, _ => new ConcurrentQueue<SpanData>());

        ReportKeyValue[]? tags = null;
        var tagCollection = activity.TagObjects.ToArray();
        if (tagCollection.Length > 0)
        {
            tags = new ReportKeyValue[tagCollection.Length];
            for (var i = 0; i < tagCollection.Length; i++)
            {
                tags[i] = new ReportKeyValue
                {
                    Key = tagCollection[i].Key,
                    Value = tagCollection[i].Value?.ToString() ?? ""
                };
            }
        }

        SpanEvent[]? events = null;
        var eventCollection = activity.Events.ToArray();
        if (eventCollection.Length > 0)
        {
            events = new SpanEvent[eventCollection.Length];
            for (var i = 0; i < eventCollection.Length; i++)
            {
                var evt = eventCollection[i];
                ReportKeyValue[]? evtTags = null;
                var evtTagCollection = evt.Tags.ToArray();
                if (evtTagCollection.Length > 0)
                {
                    evtTags = new ReportKeyValue[evtTagCollection.Length];
                    for (var j = 0; j < evtTagCollection.Length; j++)
                    {
                        evtTags[j] = new ReportKeyValue
                        {
                            Key = evtTagCollection[j].Key,
                            Value = evtTagCollection[j].Value?.ToString() ?? ""
                        };
                    }
                }

                events[i] = new SpanEvent
                {
                    Name = evt.Name,
                    TimestampMs = evt.Timestamp.ToUnixTimeMilliseconds(),
                    Tags = evtTags
                };
            }
        }

        var parentSpanId = activity.ParentSpanId != default ? activity.ParentSpanId.ToString() : null;

        var statusStr = activity.Status switch
        {
            ActivityStatusCode.Ok => "Ok",
            ActivityStatusCode.Error => "Error",
            _ => "Unset"
        };

        var spanData = new SpanData
        {
            TraceId = traceId,
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = parentSpanId,
            Name = EnrichSpanName(activity),
            SpanType = activity.DisplayName,
            Source = activity.Source.Name,
            Kind = activity.Kind.ToString(),
            StartTimeMs = activity.StartTimeUtc.Subtract(DateTime.UnixEpoch).TotalMilliseconds,
            DurationMs = activity.Duration.TotalMilliseconds,
            Status = statusStr,
            StatusMessage = activity.StatusDescription,
            Tags = tags,
            Events = events
        };

        queue.Enqueue(spanData);

        // Cleanup: remove test case span from tracking sets once it stops.
        // All child spans will have already stopped by this point (children stop before parents).
        if (isTUnit && activity.GetTagItem("tunit.test.node_uid") is not null)
        {
            var spanId = activity.SpanId.ToString();
            _testCaseSpanIds.TryRemove(spanId, out _);
            _externalSpanCountsByTest.TryRemove(spanId, out _);
            _externalSpanCountsByTrace.TryRemove(traceId, out _);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
#endif
