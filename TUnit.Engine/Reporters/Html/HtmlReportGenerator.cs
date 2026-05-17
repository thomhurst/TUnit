using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using TUnit.Core;

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
        if (end < 0) return raw;
        return raw.Remove(begin, end + SampleDataEndMarker.Length - begin);
    }

    private static string GzipBase64(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
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

        // 1) First pass: parse each test's unix-ms start, track run bounds, and stash
        //    raw (id, absStartMs, dur) so the second pass can subtract runStartMs once.
        long runStartMs = long.MaxValue;
        long runEndMs = long.MinValue;
        var rawStarts = new (string Id, long? AbsStartMs, double Dur)[totalTests];
        var ti = 0;
        foreach (var g in data.Groups)
        {
            foreach (var t in g.Tests)
            {
                var sms = TryParseUnixMs(t.StartTime);
                rawStarts[ti++] = (t.Id, sms, t.DurationMs);
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
        for (var i = 0; i < totalTests; i++)
        {
            var (id, abs, dur) = rawStarts[i];
            ordered[i] = (id, abs is { } a ? a - runStartMs : 0L, dur);
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
            var idx = 0;
            foreach (var g in data.Groups)
            {
                foreach (var t in g.Tests)
                {
                    var abs = rawStarts[idx++].AbsStartMs;
                    var startRel = abs is { } a ? a - runStartMs : 0L;
                    WriteTest(w, t, g, runStartMs, startRel, testWorker, spansByTrace);
                }
            }
            w.WriteEndArray();

            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
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

        w.WritePropertyName("spans");
        w.WriteStartArray();
        if (spansByTrace is not null)
        {
            WriteTraceSpans(w, t.TraceId, runStartMs, spansByTrace);
            if (t.AdditionalTraceIds is { Length: > 0 } extra)
            {
                foreach (var tid in extra) WriteTraceSpans(w, tid, runStartMs, spansByTrace);
            }
        }
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private static void WriteTraceSpans(
        Utf8JsonWriter w,
        string? traceId,
        long runStartMs,
        Dictionary<string, List<SpanData>> spansByTrace)
    {
        if (string.IsNullOrEmpty(traceId)) return;
        if (!spansByTrace.TryGetValue(traceId!, out var list)) return;
        foreach (var s in list)
        {
            WriteSpan(w, s, runStartMs);
        }
    }

    private static void WriteSpan(Utf8JsonWriter w, SpanData s, long runStartMs)
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
