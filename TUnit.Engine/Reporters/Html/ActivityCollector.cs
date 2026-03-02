#if NET
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Engine.Reporters.Html;

internal sealed class ActivityCollector : IDisposable
{
    // Soft caps — intentionally racy for performance; may be slightly exceeded under high concurrency.
    private const int MaxSpansPerTrace = 1000;
    private const int MaxTotalSpans = 50_000;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<SpanData>> _spansByTrace = new();
    private readonly ConcurrentDictionary<string, int> _spanCountsByTrace = new();
    private ActivityListener? _listener;
    private int _totalSpanCount;

    public void Start()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = static source => IsTUnitSource(source),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = OnActivityStopped
        };

        ActivitySource.AddActivityListener(_listener);
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
                if (!span.Name.StartsWith("test case", StringComparison.Ordinal) || span.Tags is null)
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

    private static bool IsTUnitSource(ActivitySource source) =>
        source.Name.StartsWith("TUnit", StringComparison.Ordinal) ||
        source.Name.StartsWith("Microsoft.Testing", StringComparison.Ordinal);

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
                return $"{displayName}: {value}";
            }
        }

        return displayName;
    }

    private void OnActivityStopped(Activity activity)
    {
        var newTotal = Interlocked.Increment(ref _totalSpanCount);
        if (newTotal > MaxTotalSpans)
        {
            Interlocked.Decrement(ref _totalSpanCount);
            return;
        }

        var traceId = activity.TraceId.ToString();
        var traceCount = _spanCountsByTrace.AddOrUpdate(traceId, 1, (_, c) => c + 1);
        if (traceCount > MaxSpansPerTrace)
        {
            Interlocked.Decrement(ref _totalSpanCount);
            _spanCountsByTrace.AddOrUpdate(traceId, 0, (_, c) => Math.Max(0, c - 1));
            return;
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
    }

    public void Dispose()
    {
        Stop();
    }
}
#endif
