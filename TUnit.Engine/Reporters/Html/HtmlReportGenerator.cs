using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using TUnit.Core;
using TUnit.Core.Enums;

namespace TUnit.Engine.Reporters.Html;

internal static class HtmlReportGenerator
{
    private const string TemplateResourceName = "TUnit.Engine.Reporters.Html.TestReport.template.html";
    private const string DataPlaceholder = "__REPORT_DATA__";
    private const string TitlePlaceholder = "__REPORT_TITLE__";
    private const string ProjectPlaceholder = "__REPORT_PROJECT__";
    private const string SampleDataBeginMarker = "/* SAMPLE_DATA_BEGIN";
    private const string SampleDataEndMarker = "/* SAMPLE_DATA_END */";

    private static readonly Lazy<string> Template = new(LoadAndStripTemplate);

    internal static string GenerateHtml(ReportData data)
    {
        var template = Template.Value;
        var json = SerializeReport(data);
        var compressed = GzipBase64(json);
        var encodedName = WebUtility.HtmlEncode(data.AssemblyName);

        return template
            .Replace(TitlePlaceholder, "Test Report — " + encodedName)
            .Replace(ProjectPlaceholder, encodedName)
            .Replace(DataPlaceholder, compressed);
    }

    private static string LoadAndStripTemplate()
    {
        var raw = LoadTemplate();
        // The template carries a generateSampleData() block so devs can preview
        // it standalone in a browser. Strip it from shipped reports — production
        // reports always have real data and the fallback never fires.
        var begin = raw.IndexOf(SampleDataBeginMarker, StringComparison.Ordinal);
        if (begin < 0) return raw;
        var end = raw.IndexOf(SampleDataEndMarker, begin, StringComparison.Ordinal);
        if (end < 0)
        {
            // If only the begin marker is present the strip would silently ship
            // the sample block — surface the mismatch instead of producing a
            // bloated report.
            throw new InvalidOperationException(
                $"Template has '{SampleDataBeginMarker}' but no matching '{SampleDataEndMarker}'.");
        }
        return raw.Remove(begin, end + SampleDataEndMarker.Length - begin);
    }

    private static string GzipBase64(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gz.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, (int)output.Length);
    }

    private static string LoadTemplate()
    {
        var asm = typeof(HtmlReportGenerator).Assembly;
        using var stream = asm.GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException("Embedded HTML template not found: " + TemplateResourceName);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string SerializeReport(ReportData data)
    {
        var totalTests = 0;
        foreach (var g in data.Groups)
        {
            totalTests += g.Tests.Length;
        }

        // 1) First pass: parse each test's unix-ms start, track run bounds, and
        //    record the absolute start in a dictionary keyed by test id so the
        //    later emission pass can look it up directly — without relying on
        //    enumeration order matching this loop.
        long runStartMs = long.MaxValue;
        long runEndMs = long.MinValue;
        var absStartByTestId = new Dictionary<string, long?>(totalTests, StringComparer.Ordinal);
        foreach (var g in data.Groups)
        {
            foreach (var t in g.Tests)
            {
                var sms = TryParseUnixMs(t.StartTime);
                absStartByTestId[t.Id] = sms;
                if (sms is { } x)
                {
                    if (x < runStartMs) runStartMs = x;
                    var end = x + (long)Math.Round(t.DurationMs);
                    if (end > runEndMs) runEndMs = end;
                }
            }
        }
        if (runStartMs == long.MaxValue)
        {
            runStartMs = 0;
            runEndMs = (long)Math.Round(data.TotalDurationMs);
        }
        long wallMs = Math.Max((long)Math.Round(data.TotalDurationMs), runEndMs - runStartMs);

        // 2) Resolve relative starts and sort for greedy lane assignment.
        // TUnit doesn't currently emit worker IDs per test, but the Gantt
        // chart shows per-worker lanes — derive them from start/duration.
        var ordered = new (string Id, long StartRel, double Dur)[totalTests];
        var oi = 0;
        foreach (var g in data.Groups)
        {
            foreach (var t in g.Tests)
            {
                var startRel = absStartByTestId[t.Id] is { } a ? a - runStartMs : 0L;
                ordered[oi++] = (t.Id, startRel, t.DurationMs);
            }
        }
        Array.Sort(ordered, static (a, b) => a.StartRel.CompareTo(b.StartRel));

        var laneEnd = new List<double>();
        var testWorker = new Dictionary<string, int>(totalTests, StringComparer.Ordinal);
        foreach (var (id, startRel, dur) in ordered)
        {
            var lane = -1;
            for (var i = 0; i < laneEnd.Count; i++)
            {
                if (laneEnd[i] <= startRel + 0.5)
                {
                    lane = i;
                    break;
                }
            }
            if (lane == -1)
            {
                lane = laneEnd.Count;
                laneEnd.Add(0);
            }
            laneEnd[lane] = startRel + dur;
            testWorker[id] = lane;
        }
        var workers = Math.Max(1, laneEnd.Count);

        // 3) Bucket spans by traceId so we can nest them under their owning test.
        Dictionary<string, List<SpanData>>? spansByTrace = null;
        if (data.Spans is { Length: > 0 } spans)
        {
            spansByTrace = new Dictionary<string, List<SpanData>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in spans)
            {
                if (!spansByTrace.TryGetValue(s.TraceId, out var list))
                {
                    list = new List<SpanData>();
                    spansByTrace[s.TraceId] = list;
                }
                list.Add(s);
            }
        }

        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
        {
            w.WriteStartObject();
            w.WriteString("project", data.AssemblyName);
            w.WriteString("when", data.Timestamp);
            w.WriteString("runner", data.MachineName);
            w.WriteString("os", data.OperatingSystem);
            w.WriteString("runtime", data.RuntimeVersion);
            w.WriteString("tunit", data.TUnitVersion);
            if (!string.IsNullOrEmpty(data.Branch)) w.WriteString("branch", data.Branch);
            if (!string.IsNullOrEmpty(data.CommitSha)) w.WriteString("commit", data.CommitSha);
            if (!string.IsNullOrEmpty(data.PullRequestNumber)) w.WriteString("pr", "#" + data.PullRequestNumber);
            if (!string.IsNullOrEmpty(data.RepositorySlug)) w.WriteString("repository", data.RepositorySlug);
            if (!string.IsNullOrEmpty(data.Filter)) w.WriteString("filter", data.Filter);

            w.WriteNumber("startMs", runStartMs);
            w.WriteNumber("wallMs", wallMs);
            w.WriteNumber("workers", workers);

            w.WritePropertyName("tests");
            w.WriteStartArray();
            foreach (var g in data.Groups)
            {
                foreach (var t in g.Tests)
                {
                    var startRel = absStartByTestId[t.Id] is { } a ? a - runStartMs : 0L;
                    WriteTest(w, t, g, runStartMs, startRel, testWorker, spansByTrace);
                }
            }
            w.WriteEndArray();

            WriteTimelines(w, data, spansByTrace, runStartMs);

            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    // The old report had two trace views the per-test Trace tab doesn't cover:
    //   • Global "Execution Timeline" — session/assembly/suite + shared init/dispose spans
    //   • Per-class timeline — opt-in via [ClassTimeline(...)] on the class
    // We re-emit both as top-level JSON so the renderer can show them in the Run view.
    private static void WriteTimelines(Utf8JsonWriter w, ReportData data, Dictionary<string, List<SpanData>>? spansByTrace, long runStartMs)
    {
        if (spansByTrace is null || spansByTrace.Count == 0) return;

        // Single pass: collect global-scope spans and the per-class suite anchor map.
        var globalSpans = new List<SpanData>();
        var suiteByClass = new Dictionary<string, SpanData>(StringComparer.Ordinal);
        foreach (var bucket in spansByTrace.Values)
        {
            foreach (var s in bucket)
            {
                if (IsGlobalTimelineSpan(s)) globalSpans.Add(s);
                if (string.Equals(s.SpanType, TUnitActivitySource.SpanTestSuite, StringComparison.Ordinal))
                {
                    var cls = FindTagValue(s, TUnitActivitySource.TagTestClass) ?? s.Name;
                    if (!string.IsNullOrEmpty(cls) && !suiteByClass.ContainsKey(cls)) suiteByClass[cls] = s;
                }
            }
        }

        // Session/assembly/suite spans are emitted on the LifecycleSource using the
        // caller's ActivityContext as parent. When Activity.Current is unset at the
        // moment a hook fires, the parent context falls back to default and the
        // resulting ParentSpanId may not match any span we collected — leaving the
        // assembly visually flattened next to its session in the timeline. Repair
        // the chain so the renderer can draw the session → assembly → suite tree.
        globalSpans = RepairGlobalTimelineParents(globalSpans);

        w.WritePropertyName("globalSpans");
        w.WriteStartArray();
        foreach (var s in globalSpans) WriteSpan(w, s, runStartMs);
        w.WriteEndArray();

        // Per-class timeline: classes that opted in via [ClassTimeline] carry the
        // ClassTimelineAttribute.ClassTimelinePropertyKey custom property on every test.
        var classModes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var g in data.Groups)
        {
            foreach (var t in g.Tests)
            {
                if (t.CustomProperties is null) continue;
                foreach (var p in t.CustomProperties)
                {
                    if (string.Equals(p.Key, ClassTimelineAttribute.ClassTimelinePropertyKey, StringComparison.Ordinal))
                    {
                        classModes[g.ClassName] = p.Value;
                        break;
                    }
                }
                if (classModes.ContainsKey(g.ClassName)) break;
            }
        }

        w.WritePropertyName("classTimelines");
        w.WriteStartObject();
        // Cache the parent/child index per trace so multiple opted-in classes sharing a
        // trace don't rebuild it each time.
        var traceIndexCache = new Dictionary<string, (Dictionary<string, SpanData> BySpanId, Dictionary<string, List<SpanData>> ByParent)>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in classModes)
        {
            if (!suiteByClass.TryGetValue(kv.Key, out var suite)) continue;
            if (!spansByTrace.TryGetValue(suite.TraceId, out var traceSpans)) continue;

            if (!traceIndexCache.TryGetValue(suite.TraceId, out var index))
            {
                index = BuildTraceIndex(traceSpans);
                traceIndexCache[suite.TraceId] = index;
            }

            var classSpans = BuildClassTimeline(index.BySpanId, index.ByParent, suite.SpanId, kv.Value);
            if (classSpans.Count == 0) continue;

            w.WritePropertyName(kv.Key);
            w.WriteStartObject();
            w.WriteString("mode", kv.Value);
            w.WritePropertyName("spans");
            w.WriteStartArray();
            foreach (var s in classSpans) WriteSpan(w, s, runStartMs);
            w.WriteEndArray();
            w.WriteEndObject();
        }
        w.WriteEndObject();
    }

    private static (Dictionary<string, SpanData> BySpanId, Dictionary<string, List<SpanData>> ByParent) BuildTraceIndex(List<SpanData> traceSpans)
    {
        var bySpanId = new Dictionary<string, SpanData>(traceSpans.Count, StringComparer.OrdinalIgnoreCase);
        var byParent = new Dictionary<string, List<SpanData>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in traceSpans)
        {
            bySpanId[s.SpanId] = s;
            if (s.ParentSpanId is null) continue;
            if (!byParent.TryGetValue(s.ParentSpanId, out var kids))
            {
                kids = new List<SpanData>();
                byParent[s.ParentSpanId] = kids;
            }
            kids.Add(s);
        }
        return (bySpanId, byParent);
    }

    private static List<SpanData> RepairGlobalTimelineParents(List<SpanData> spans)
    {
        if (spans.Count == 0) return spans;

        SpanData? session = null;
        SpanData? firstAssembly = null;
        var presentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in spans)
        {
            presentIds.Add(s.SpanId);
            if (session is null && string.Equals(s.SpanType, TUnitActivitySource.SpanTestSession, StringComparison.Ordinal)) session = s;
            if (firstAssembly is null && string.Equals(s.SpanType, TUnitActivitySource.SpanTestAssembly, StringComparison.Ordinal)) firstAssembly = s;
        }

        var repaired = new List<SpanData>(spans.Count);
        foreach (var s in spans)
        {
            if (ReferenceEquals(s, session)
                || (!string.IsNullOrEmpty(s.ParentSpanId) && presentIds.Contains(s.ParentSpanId)))
            {
                repaired.Add(s);
                continue;
            }
            var fallback = string.Equals(s.SpanType, TUnitActivitySource.SpanTestSuite, StringComparison.Ordinal)
                ? firstAssembly?.SpanId ?? session?.SpanId
                : session?.SpanId;
            repaired.Add(fallback is not null ? WithParent(s, fallback) : s);
        }
        return repaired;
    }

    private static bool IsGlobalTimelineSpan(SpanData s)
    {
        var t = s.SpanType ?? string.Empty;
        if (t == TUnitActivitySource.SpanTestSession
            || t == TUnitActivitySource.SpanTestAssembly
            || t == TUnitActivitySource.SpanTestSuite) return true;
        if (t.StartsWith("initialize ", StringComparison.Ordinal) || t.StartsWith("dispose ", StringComparison.Ordinal))
        {
            var scope = FindTagValue(s, TUnitActivitySource.TagTraceScope);
            return !string.Equals(scope, "test", StringComparison.Ordinal);
        }
        return false;
    }

    private static List<SpanData> BuildClassTimeline(Dictionary<string, SpanData> bySpanId, Dictionary<string, List<SpanData>> byParent, string suiteSpanId, string mode)
    {
        var descendants = new List<SpanData>();
        var stack = new Stack<string>();
        stack.Push(suiteSpanId);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!visited.Add(id)) continue;
            if (byParent.TryGetValue(id, out var kids))
            {
                foreach (var k in kids)
                {
                    descendants.Add(k);
                    stack.Push(k.SpanId);
                }
            }
        }

        var result = new List<SpanData>();
        if (bySpanId.TryGetValue(suiteSpanId, out var suite)) result.Add(suite);

        if (string.Equals(mode, nameof(TimelineMode.FullExecution), StringComparison.Ordinal))
        {
            // Include test-case spans and their non-'test body' children, with
            // 'test body' wrappers collapsed (children re-parented to the owning
            // test-case) so the timeline isn't dominated by plumbing nodes.
            var testBodyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in descendants)
                if (string.Equals(s.Name, TUnitActivitySource.SpanTestBody, StringComparison.Ordinal)) testBodyIds.Add(s.SpanId);
            foreach (var s in descendants)
            {
                if (testBodyIds.Contains(s.SpanId)) continue;
                if (s.ParentSpanId is not null && testBodyIds.Contains(s.ParentSpanId))
                {
                    var newParent = bySpanId.TryGetValue(s.ParentSpanId, out var body) ? body.ParentSpanId : null;
                    result.Add(WithParent(s, newParent));
                }
                else
                {
                    result.Add(s);
                }
            }
        }
        else
        {
            // Default: class-level infrastructure only — drop test-case spans and
            // everything beneath them so the timeline shows suite + init/dispose only.
            var testCaseIds = new List<string>();
            foreach (var s in descendants)
                if (string.Equals(s.SpanType, TUnitActivitySource.SpanTestCase, StringComparison.Ordinal)) testCaseIds.Add(s.SpanId);
            var excluded = new HashSet<string>(testCaseIds, StringComparer.OrdinalIgnoreCase);
            foreach (var tcId in testCaseIds)
            {
                var inner = new Stack<string>();
                inner.Push(tcId);
                while (inner.Count > 0)
                {
                    var id = inner.Pop();
                    if (!byParent.TryGetValue(id, out var kids)) continue;
                    foreach (var k in kids) { if (excluded.Add(k.SpanId)) inner.Push(k.SpanId); }
                }
            }
            foreach (var s in descendants)
                if (!excluded.Contains(s.SpanId)) result.Add(s);
        }

        return result;
    }

    private static SpanData WithParent(SpanData s, string? parentSpanId) => new()
    {
        TraceId = s.TraceId,
        SpanId = s.SpanId,
        ParentSpanId = parentSpanId,
        Name = s.Name,
        SpanType = s.SpanType,
        Source = s.Source,
        Kind = s.Kind,
        StartTimeMs = s.StartTimeMs,
        DurationMs = s.DurationMs,
        Status = s.Status,
        StatusMessage = s.StatusMessage,
        Tags = s.Tags,
        Events = s.Events,
        Links = s.Links,
    };

    private static string? FindTagValue(SpanData s, string key)
    {
        if (s.Tags is null) return null;
        foreach (var t in s.Tags)
            if (string.Equals(t.Key, key, StringComparison.Ordinal)) return t.Value;
        return null;
    }

    private static void WriteTest(
        Utf8JsonWriter w,
        ReportTestResult t,
        ReportTestGroup g,
        long runStartMs,
        long startRel,
        Dictionary<string, int> testWorker,
        Dictionary<string, List<SpanData>>? spansByTrace)
    {
        w.WriteStartObject();
        w.WriteString("id", t.Id);
        w.WriteString("name", t.DisplayName);
        w.WriteString("cls", g.ClassName);
        w.WriteString("ns", g.Namespace);
        w.WriteString("status", MapStatus(t.Status));
        w.WriteNumber("start", startRel);
        w.WriteNumber("duration", t.DurationMs);
        w.WriteString("worker", "worker-" + (testWorker.GetValueOrDefault(t.Id, 0) + 1));
        if (!string.IsNullOrEmpty(t.TraceId)) w.WriteString("traceId", t.TraceId);

        // properties — design schema is an object map; dedupe duplicate keys (first wins).
        w.WritePropertyName("properties");
        w.WriteStartObject();
        if (t.CustomProperties is { Length: > 0 } props)
        {
            // First occurrence of a duplicated key wins.
            var emitted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in props)
            {
                if (emitted.Add(p.Key)) w.WriteString(p.Key, p.Value);
            }
        }
        w.WriteEndObject();

        w.WritePropertyName("categories");
        w.WriteStartArray();
        if (t.Categories is { Length: > 0 } cats)
        {
            foreach (var c in cats) w.WriteStringValue(c);
        }
        w.WriteEndArray();

        w.WriteString("stdout", t.Output ?? string.Empty);
        // Engine-emitted advisories (e.g. "[TUnit] External span cap reached...") are written
        // to Console.Error and end up in whichever test was running. They aren't test failures,
        // so strip them before the renderer counts them in the "N err" tab badge.
        var stderr = FilterEngineNotices(t.ErrorOutput) ?? t.SkipReason ?? string.Empty;
        w.WriteString("stderr", stderr);

        if (t.Exception is not null)
        {
            w.WritePropertyName("error");
            WriteException(w, t.Exception);
        }

        w.WritePropertyName("source");
        w.WriteStartObject();
        if (!string.IsNullOrEmpty(t.FilePath)) w.WriteString("path", t.FilePath);
        if (t.LineNumber is { } ln) w.WriteNumber("line", ln);
        w.WriteEndObject();

        if (t.RetryAttempt > 0)
        {
            w.WriteNumber("retryCount", t.RetryAttempt);
        }

        if (t.Attempts is { Length: > 1 } atts)
        {
            w.WritePropertyName("attempts");
            w.WriteStartArray();
            foreach (var a in atts)
            {
                w.WriteStartObject();
                w.WriteString("status", MapStatus(a.Status));
                w.WriteNumber("duration", a.DurationMs);
                if (!string.IsNullOrEmpty(a.ExceptionType) || !string.IsNullOrEmpty(a.ExceptionMessage))
                {
                    w.WritePropertyName("error");
                    w.WriteStartObject();
                    if (!string.IsNullOrEmpty(a.ExceptionType)) w.WriteString("type", a.ExceptionType!);
                    if (!string.IsNullOrEmpty(a.ExceptionMessage)) w.WriteString("message", a.ExceptionMessage!);
                    w.WriteEndObject();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }

        w.WritePropertyName("spans");
        w.WriteStartArray();
        if (spansByTrace is not null)
        {
            WriteTraceSpans(w, t.TraceId, runStartMs, spansByTrace, linked: false);
            if (t.AdditionalTraceIds is { Length: > 0 } extra)
            {
                foreach (var tid in extra) WriteTraceSpans(w, tid, runStartMs, spansByTrace, linked: true);
            }
        }
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private static void WriteTraceSpans(
        Utf8JsonWriter w,
        string? traceId,
        long runStartMs,
        Dictionary<string, List<SpanData>> spansByTrace,
        bool linked = false)
    {
        if (string.IsNullOrEmpty(traceId)) return;
        if (!spansByTrace.TryGetValue(traceId!, out var list)) return;
        foreach (var s in list)
        {
            WriteSpan(w, s, runStartMs, linked);
        }
    }

    private static void WriteSpan(Utf8JsonWriter w, SpanData s, long runStartMs, bool linked = false)
    {
        w.WriteStartObject();
        w.WriteString("id", s.SpanId);
        if (s.ParentSpanId is { Length: > 0 })
        {
            w.WriteString("parent", s.ParentSpanId);
        }
        else
        {
            w.WriteNull("parent");
        }
        w.WriteString("name", s.Name);
        w.WriteString("service", MapSpanService(s));
        var startRel = s.StartTimeMs - runStartMs;
        w.WriteNumber("start", startRel < 0 ? 0 : startRel);
        w.WriteNumber("dur", s.DurationMs);
        w.WritePropertyName("attrs");
        w.WriteStartObject();
        if (s.Tags is { Length: > 0 } tags)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                if (seen.Add(tag.Key)) w.WriteString(tag.Key, tag.Value);
            }
        }
        w.WriteEndObject();
        if (s.Status is { Length: > 0 } && !string.Equals(s.Status, "Unset", StringComparison.Ordinal))
        {
            w.WriteString("status", s.Status);
        }
        if (s.Events is { Length: > 0 } events)
        {
            w.WritePropertyName("events");
            w.WriteStartArray();
            foreach (var ev in events)
            {
                w.WriteStartObject();
                w.WriteString("name", ev.Name);
                w.WriteNumber("time", ev.TimestampMs - runStartMs);
                if (ev.Tags is { Length: > 0 } evTags)
                {
                    w.WritePropertyName("attrs");
                    w.WriteStartObject();
                    var seenEv = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var t in evTags)
                    {
                        if (seenEv.Add(t.Key)) w.WriteString(t.Key, t.Value);
                    }
                    w.WriteEndObject();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        if (linked) w.WriteBoolean("linked", true);
        w.WriteEndObject();
    }

    private static void WriteException(Utf8JsonWriter w, ReportExceptionData ex)
    {
        w.WriteStartObject();
        w.WriteString("type", ex.Type);
        w.WriteString("message", ex.Message);
        if (!string.IsNullOrEmpty(ex.StackTrace)) w.WriteString("stack", ex.StackTrace);
        TryExtractExpectedActual(ex.Message, out var expected, out var actual);
        if (expected is not null) w.WriteString("expected", expected);
        if (actual is not null) w.WriteString("actual", actual);
        if (ex.InnerException is not null)
        {
            w.WritePropertyName("innerException");
            WriteException(w, ex.InnerException);
        }
        w.WriteEndObject();
    }

    // TUnit assertion messages often follow "Expected: X\n  Actual: Y" — light parsing
    // lets the renderer show a real Expected/Actual diff block instead of raw text.
    private static void TryExtractExpectedActual(string message, out string? expected, out string? actual)
    {
        expected = null;
        actual = null;
        if (string.IsNullOrEmpty(message)) return;
        var lines = message.Replace("\r\n", "\n").Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.TrimStart();
            if (expected is null && line.StartsWith("Expected:", StringComparison.OrdinalIgnoreCase))
            {
                expected = line.Substring("Expected:".Length).Trim();
            }
            else if (actual is null && (line.StartsWith("Actual:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("But was:", StringComparison.OrdinalIgnoreCase)))
            {
                var prefixLen = line.StartsWith("Actual:", StringComparison.OrdinalIgnoreCase) ? "Actual:".Length : "But was:".Length;
                actual = line.Substring(prefixLen).Trim();
            }
        }
        if (expected is { Length: 0 }) expected = null;
        if (actual is { Length: 0 }) actual = null;
    }

    private static string MapSpanService(SpanData s)
    {
        if (s.Tags is { Length: > 0 } tags)
        {
            string? dbSystem = null;
            string? msgSystem = null;
            var hasHttp = false;
            foreach (var t in tags)
            {
                switch (t.Key)
                {
                    case "db.system":
                        dbSystem = t.Value;
                        break;
                    case "messaging.system":
                        msgSystem = t.Value;
                        break;
                    default:
                        if (t.Key.StartsWith("http.", StringComparison.Ordinal)) hasHttp = true;
                        break;
                }
            }
            if (!string.IsNullOrEmpty(dbSystem))
            {
                return string.Equals(dbSystem, "postgresql", StringComparison.OrdinalIgnoreCase) ? "npgsql" : dbSystem!;
            }
            if (!string.IsNullOrEmpty(msgSystem)) return msgSystem!;
            if (hasHttp)
            {
                return string.Equals(s.Kind, "Server", StringComparison.OrdinalIgnoreCase) ? "http.server" : "http.client";
            }
        }
        if (!string.IsNullOrEmpty(s.Source))
        {
            var src = s.Source.ToLowerInvariant();
            if (src.Contains("npgsql")) return "npgsql";
            if (src.Contains("httpclient") || src.Contains("http.client")) return "http.client";
            if (src.Contains("aspnetcore") || src.Contains("kestrel") || src.Contains("http.server")) return "http.server";
            if (src.Contains("rabbit")) return "rabbitmq";
            if (src.Contains("tunit")) return "test";
        }
        if (s.Name is { Length: > 0 } name)
        {
            if (name.StartsWith("hook", StringComparison.OrdinalIgnoreCase) || name.IndexOf("hook:", StringComparison.OrdinalIgnoreCase) >= 0) return "hook";
        }
        return string.IsNullOrEmpty(s.Source) ? "test" : s.Source.ToLowerInvariant();
    }

    private static string MapStatus(string status) => status switch
    {
        "passed" => "pass",
        "failed" or "error" or "timedOut" => "fail",
        "skipped" => "skip",
        "cancelled" => "cancel",
        // Unknown statuses (future engine values, "unknown") map to fail so they
        // stay visible in the UI rather than being silently buried under skipped.
        _ => "fail",
    };

    private static string? FilterEngineNotices(string? stderr)
    {
        if (string.IsNullOrEmpty(stderr)) return stderr;
        if (stderr!.IndexOf("[TUnit]", StringComparison.Ordinal) < 0) return stderr;
        var lines = stderr.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(stderr.Length);
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("[TUnit]", StringComparison.Ordinal)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static long? TryParseUnixMs(string? iso)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        return DateTimeOffset.TryParse(
            iso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var dt)
            ? dt.ToUnixTimeMilliseconds()
            : null;
    }
}
