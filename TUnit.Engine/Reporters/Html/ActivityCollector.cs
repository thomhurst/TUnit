#if NET
using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core;
using TUnit.Engine.Configuration;

namespace TUnit.Engine.Reporters.Html;

internal sealed class ActivityCollector : IDisposable
{
    // Cap external (non-TUnit) spans to keep the report manageable. Applied per test,
    // or per trace when the test-case association can't be determined (e.g. broken
    // Activity.Parent chains from async connection pooling). TUnit's own spans are
    // always captured regardless. Soft cap — intentionally racy for performance; may
    // be slightly exceeded under high concurrency. Override via
    // EnvironmentConstants.MaxOtelExternalSpans for users with busy SUTs.
    private const int DefaultMaxExternalSpans = 100;
    private static readonly int MaxExternalSpans = ResolveExternalSpanCap();

    private static int _capWarningEmitted;

    private static int ResolveExternalSpanCap()
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentConstants.MaxOtelExternalSpans);
        if (!string.IsNullOrEmpty(raw)
            && int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return DefaultMaxExternalSpans;
    }

    private static void WarnCapHitOnce()
    {
        // CompareExchange avoids re-writing the flag on every overflow span once the
        // cap has been breached — on busy SUTs this runs at every dropped span.
        if (Interlocked.CompareExchange(ref _capWarningEmitted, 1, 0) == 0)
        {
            Console.Error.WriteLine(
                $"[TUnit] External span cap of {MaxExternalSpans} reached; subsequent spans will be dropped. " +
                $"Set {EnvironmentConstants.MaxOtelExternalSpans} to raise the limit.");
        }
    }

    // Process-wide pointer to the currently-running collector, used by the OTLP receiver
    // (in TUnit.OpenTelemetry) to feed external spans without an explicit wiring step.
    // Only one HtmlReporter runs per session, so a static slot is sufficient.
    private static ActivityCollector? _current;

    public static ActivityCollector? Current => _current;

    // All trace/span ID dictionaries use OrdinalIgnoreCase because external spans
    // arrive hex-encoded as uppercase (Convert.ToHexString) while in-process Activity
    // IDs serialize lowercase. Without case-insensitive keys the two would split into
    // separate buckets for the same logical trace.
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SpanData>> _spansByTrace = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _externalSpanCountsByTest = new(StringComparer.OrdinalIgnoreCase);
    // Fallback per-trace cap for external spans whose parent chain is broken
    // (e.g. Npgsql async pooling where Activity.Parent is null but traceId is correct).
    private readonly ConcurrentDictionary<string, int> _externalSpanCountsByTrace = new(StringComparer.OrdinalIgnoreCase);
    // Known test case span IDs, populated at activity start time so they're available
    // before child spans stop (children stop before parents in Activity ordering).
    private readonly ConcurrentDictionary<string, byte> _testCaseSpanIds = new(StringComparer.OrdinalIgnoreCase);
    // Fast-path cache of trace IDs that should be collected. Subsumes TraceRegistry lookups
    // so that subsequent activities on the same trace avoid cross-class dictionary checks.
    private readonly ConcurrentDictionary<string, byte> _knownTraceIds = new(StringComparer.OrdinalIgnoreCase);
    private ActivityListener? _listener;

    public void Start()
    {
        // First-started-wins: HtmlReporter creates one collector per session before any
        // test runs, so this slot is claimed for the rest of the session. Later ad-hoc
        // collectors (e.g. created from a test) don't race-steal the global pointer.
        Interlocked.CompareExchange(ref _current, this, null);
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
        Interlocked.CompareExchange(ref _current, null, this);
        _listener?.Dispose();
        _listener = null;
    }

    /// <summary>
    /// Marks a trace ID as eligible for external span ingestion. Called by the OTLP
    /// receiver when the test process has started or observed a trace that external
    /// processes (e.g. a WebApplicationFactory SUT) may report spans against.
    /// </summary>
    internal void RegisterExternalTrace(string traceId)
    {
        _knownTraceIds.TryAdd(traceId, 0);
    }

    /// <summary>
    /// Enqueues an externally-sourced span (typically from an OTLP receiver) into
    /// the report. Dropped if the trace is not known, or if per-test/per-trace caps
    /// for external spans have been exceeded.
    /// </summary>
    internal void IngestExternalSpan(SpanData span)
    {
        if (!_knownTraceIds.ContainsKey(span.TraceId))
        {
            return;
        }

        // Prefer per-test cap when the span's direct parent is a known test case span.
        // Falls back to per-trace cap otherwise, mirroring OnActivityStopped's logic.
        if (span.ParentSpanId is { } parentSpanId && _testCaseSpanIds.ContainsKey(parentSpanId))
        {
            if (_externalSpanCountsByTest.TryGetValue(parentSpanId, out var existing) && existing >= MaxExternalSpans)
            {
                WarnCapHitOnce();
                return;
            }

            var count = _externalSpanCountsByTest.AddOrUpdate(parentSpanId, 1, static (_, c) => c + 1);
            if (count > MaxExternalSpans)
            {
                WarnCapHitOnce();
                return;
            }
        }
        else
        {
            if (_externalSpanCountsByTrace.TryGetValue(span.TraceId, out var existing) && existing >= MaxExternalSpans)
            {
                WarnCapHitOnce();
                return;
            }

            var count = _externalSpanCountsByTrace.AddOrUpdate(span.TraceId, 1, static (_, c) => c + 1);
            if (count > MaxExternalSpans)
            {
                WarnCapHitOnce();
                return;
            }
        }

        var queue = _spansByTrace.GetOrAdd(span.TraceId, static _ => new ConcurrentQueue<SpanData>());
        queue.Enqueue(span);
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
                    if (tag.Key == TUnitActivitySource.TagTestNodeUid && !string.IsNullOrEmpty(tag.Value))
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
        // Register TUnit activities' trace IDs early so child spans from other sources
        // (HttpClient, EF Core, etc.) can be sampled correctly. This is especially
        // important for test case activities that start their own trace (parentContext: default),
        // since their traceId is only assigned by the runtime after StartActivity returns.
        if (IsTUnitSource(activity.Source.Name))
        {
            _knownTraceIds.TryAdd(activity.TraceId.ToString(), 0);

            // Register test case span IDs early so they're available for child span lookups.
            // Children stop before parents in Activity ordering, so we need this pre-registered.
            if (activity.GetTagItem(TUnitActivitySource.TagTestNodeUid) is not null)
            {
                _testCaseSpanIds.TryAdd(activity.SpanId.ToString(), 0);
            }
        }
    }

    private string? FindTestCaseAncestor(Activity activity)
    {
        // First: walk in-memory parent chain (works when parent Activity is alive)
        var current = activity.Parent;
        while (current is not null)
        {
            if (IsTUnitSource(current.Source.Name) &&
                current.GetTagItem(TUnitActivitySource.TagTestNodeUid) is not null)
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
            TUnitActivitySource.SpanTestCase => TUnitActivitySource.TagTestCaseName,
            TUnitActivitySource.SpanTestSuite => TUnitActivitySource.TagTestSuiteName,
            TUnitActivitySource.SpanTestAssembly => TUnitActivitySource.TagAssemblyName,
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
                if (count > MaxExternalSpans)
                {
                    WarnCapHitOnce();
                    return;
                }
            }
            else
            {
                // Fallback cap by trace ID to prevent unbounded growth for spans
                // with broken parent chains (e.g., Npgsql async connection pooling).
                var count = _externalSpanCountsByTrace.AddOrUpdate(traceId, 1, (_, c) => c + 1);
                if (count > MaxExternalSpans)
                {
                    WarnCapHitOnce();
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

        SpanLink[]? links = null;
        var activityLinks = activity.Links.ToArray();
        if (activityLinks.Length > 0)
        {
            links = new SpanLink[activityLinks.Length];
            for (var i = 0; i < activityLinks.Length; i++)
            {
                links[i] = new SpanLink
                {
                    TraceId = activityLinks[i].Context.TraceId.ToString(),
                    SpanId = activityLinks[i].Context.SpanId.ToString()
                };
            }
        }

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
            Events = events,
            Links = links
        };

        queue.Enqueue(spanData);

        // Cleanup: remove test case span from tracking sets once it stops.
        // All child spans will have already stopped by this point (children stop before parents).
        if (isTUnit && activity.GetTagItem(TUnitActivitySource.TagTestNodeUid) is not null)
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
