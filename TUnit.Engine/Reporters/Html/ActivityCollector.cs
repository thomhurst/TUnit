#if NET
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TUnit.Engine.Reporters.Html;

internal sealed class ActivityCollector : IDisposable
{
    private const int MaxSpansPerTrace = 1000;

    private readonly ConcurrentDictionary<string, ConcurrentBag<SpanData>> _spansByTrace = new();
    private ActivityListener? _listener;

    public void Start()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
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
        var all = new List<SpanData>();
        foreach (var kvp in _spansByTrace)
        {
            all.AddRange(kvp.Value);
        }

        return all.ToArray();
    }

    private void OnActivityStopped(Activity activity)
    {
        var traceId = activity.TraceId.ToString();
        var bag = _spansByTrace.GetOrAdd(traceId, _ => new ConcurrentBag<SpanData>());

        if (bag.Count >= MaxSpansPerTrace)
        {
            return;
        }

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
            Name = activity.DisplayName,
            Source = activity.Source.Name,
            Kind = activity.Kind.ToString(),
            StartTimeMs = activity.StartTimeUtc.Subtract(DateTime.UnixEpoch).TotalMilliseconds,
            DurationMs = activity.Duration.TotalMilliseconds,
            Status = statusStr,
            StatusMessage = activity.StatusDescription,
            Tags = tags,
            Events = events
        };

        bag.Add(spanData);
    }

    public void Dispose()
    {
        Stop();
    }
}
#endif
