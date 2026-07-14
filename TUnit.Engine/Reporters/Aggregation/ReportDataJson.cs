using System.IO;
using System.Text;
using System.Text.Json;
using TUnit.Core;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Canonical JSON persistence for <see cref="ReportData"/> — the machine-readable
/// sidecar written next to the HTML report and consumed by cross-process aggregation
/// (the in-engine cooperative merge and the <c>tunit-report</c> dotnet tool).
///
/// This is a different schema from the renderer JSON embedded in the HTML report:
/// that one is lossy (statuses collapsed, times made relative) and shaped for the
/// template's client script. This one round-trips <see cref="ReportData"/> faithfully,
/// using the property names documented by the <c>[JsonPropertyName]</c> attributes on
/// the model. Both sides are hand-written (no reflection serializer) so they stay
/// Native-AOT compatible. Unknown properties are ignored on read for forward compat.
/// </summary>
internal static class ReportDataJson
{
    /// <summary>Bump when the schema changes shape incompatibly; readers reject newer majors.</summary>
    internal const int SchemaVersion = 1;

    /// <summary>File extension shared by every sidecar so aggregators can discover them.</summary>
    internal const string SidecarExtension = ".tunit-report.json";

    internal static string Serialize(ReportData data)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
        {
            Write(w, data);
        }
        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, checked((int)ms.Length));
    }

    private static void Write(Utf8JsonWriter w, ReportData data)
    {
        w.WriteStartObject();
        w.WriteNumber("schemaVersion", SchemaVersion);
        w.WriteString("assemblyName", data.AssemblyName);
        w.WriteString("machineName", data.MachineName);
        w.WriteString("timestamp", data.Timestamp);
        w.WriteString("tunitVersion", data.TUnitVersion);
        w.WriteString("operatingSystem", data.OperatingSystem);
        w.WriteString("runtimeVersion", data.RuntimeVersion);
        if (data.Filter is not null) w.WriteString("filter", data.Filter);
        w.WriteNumber("totalDurationMs", data.TotalDurationMs);
        if (data.ArtifactUrl is not null) w.WriteString("artifactUrl", data.ArtifactUrl);
        if (data.CommitSha is not null) w.WriteString("commitSha", data.CommitSha);
        if (data.Branch is not null) w.WriteString("branch", data.Branch);
        if (data.PullRequestNumber is not null) w.WriteString("pullRequestNumber", data.PullRequestNumber);
        if (data.RepositorySlug is not null) w.WriteString("repositorySlug", data.RepositorySlug);
        if (data.SourceLinks is { } links)
        {
            // Persisted so a merged report keeps clickable source links: the HTML template
            // renders them from these URL templates, not from commit/repository fields.
            w.WritePropertyName("sourceLinks");
            w.WriteStartObject();
            w.WriteString("lineUrl", links.LineUrl);
            w.WriteString("rangeUrl", links.RangeUrl);
            if (links.RawUrl is not null) w.WriteString("rawUrl", links.RawUrl);
            w.WriteEndObject();
        }

        w.WritePropertyName("summary");
        WriteSummary(w, data.Summary);

        w.WritePropertyName("groups");
        w.WriteStartArray();
        foreach (var g in data.Groups)
        {
            w.WriteStartObject();
            w.WriteString("className", g.ClassName);
            w.WriteString("namespace", g.Namespace);
            w.WritePropertyName("summary");
            WriteSummary(w, g.Summary);
            w.WritePropertyName("tests");
            w.WriteStartArray();
            foreach (var t in g.Tests) WriteTest(w, t);
            w.WriteEndArray();
            w.WriteEndObject();
        }
        w.WriteEndArray();

        if (data.Spans is { Length: > 0 } spans)
        {
            w.WritePropertyName("spans");
            w.WriteStartArray();
            foreach (var s in spans) WriteSpan(w, s);
            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

    private static void WriteSummary(Utf8JsonWriter w, ReportSummary s)
    {
        w.WriteStartObject();
        w.WriteNumber("total", s.Total);
        w.WriteNumber("passed", s.Passed);
        w.WriteNumber("failed", s.Failed);
        w.WriteNumber("skipped", s.Skipped);
        w.WriteNumber("cancelled", s.Cancelled);
        w.WriteNumber("timedOut", s.TimedOut);
        w.WriteNumber("flaky", s.Flaky);
        w.WriteEndObject();
    }

    private static void WriteTest(Utf8JsonWriter w, ReportTestResult t)
    {
        w.WriteStartObject();
        w.WriteString("id", t.Id);
        w.WriteString("displayName", t.DisplayName);
        w.WriteString("methodName", t.MethodName);
        w.WriteString("className", t.ClassName);
        w.WriteString("status", t.Status);
        w.WriteNumber("durationMs", t.DurationMs);
        if (t.StartTime is not null) w.WriteString("startTime", t.StartTime);
        if (t.EndTime is not null) w.WriteString("endTime", t.EndTime);
        if (t.Exception is not null)
        {
            w.WritePropertyName("exception");
            WriteException(w, t.Exception);
        }
        if (t.Output is not null) w.WriteString("output", t.Output);
        if (t.ErrorOutput is not null) w.WriteString("errorOutput", t.ErrorOutput);
        if (t.Categories is { Length: > 0 } cats)
        {
            w.WritePropertyName("categories");
            w.WriteStartArray();
            foreach (var c in cats) w.WriteStringValue(c);
            w.WriteEndArray();
        }
        if (t.CustomProperties is { Length: > 0 } props)
        {
            w.WritePropertyName("customProperties");
            w.WriteStartArray();
            foreach (var p in props) WriteKeyValue(w, p);
            w.WriteEndArray();
        }
        if (t.FilePath is not null) w.WriteString("filePath", t.FilePath);
        if (t.LineNumber is { } line) w.WriteNumber("lineNumber", line);
        if (t.EndLineNumber is { } endLine) w.WriteNumber("endLineNumber", endLine);
        if (t.SourceRelativePath is not null) w.WriteString("sourceRelativePath", t.SourceRelativePath);
        if (t.SkipReason is not null) w.WriteString("skipReason", t.SkipReason);
        if (t.RetryAttempt != 0) w.WriteNumber("retryAttempt", t.RetryAttempt);
        if (t.Attempts is { Length: > 0 } attempts)
        {
            w.WritePropertyName("attempts");
            w.WriteStartArray();
            foreach (var a in attempts)
            {
                w.WriteStartObject();
                w.WriteString("status", a.Status);
                w.WriteNumber("durationMs", a.DurationMs);
                if (a.ExceptionType is not null) w.WriteString("exceptionType", a.ExceptionType);
                if (a.ExceptionMessage is not null) w.WriteString("exceptionMessage", a.ExceptionMessage);
                if (a.StackTrace is not null) w.WriteString("stackTrace", a.StackTrace);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        if (t.TraceId is not null) w.WriteString("traceId", t.TraceId);
        if (t.SpanId is not null) w.WriteString("spanId", t.SpanId);
        if (t.AdditionalTraceIds is { Length: > 0 } extra)
        {
            w.WritePropertyName("additionalTraceIds");
            w.WriteStartArray();
            foreach (var id in extra) w.WriteStringValue(id);
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    private static void WriteException(Utf8JsonWriter w, ReportExceptionData ex)
    {
        w.WriteStartObject();
        w.WriteString("type", ex.Type);
        w.WriteString("message", ex.Message);
        if (ex.StackTrace is not null) w.WriteString("stackTrace", ex.StackTrace);
        if (ex.InnerException is not null)
        {
            w.WritePropertyName("innerException");
            WriteException(w, ex.InnerException);
        }
        w.WriteEndObject();
    }

    private static void WriteSpan(Utf8JsonWriter w, SpanData s)
    {
        w.WriteStartObject();
        w.WriteString("traceId", s.TraceId);
        w.WriteString("spanId", s.SpanId);
        if (s.ParentSpanId is not null) w.WriteString("parentSpanId", s.ParentSpanId);
        w.WriteString("name", s.Name);
        if (s.SpanType is not null) w.WriteString("spanType", s.SpanType);
        w.WriteString("source", s.Source);
        w.WriteString("kind", s.Kind);
        w.WriteNumber("startTimeMs", s.StartTimeMs);
        w.WriteNumber("durationMs", s.DurationMs);
        w.WriteString("status", s.Status);
        if (s.StatusMessage is not null) w.WriteString("statusMessage", s.StatusMessage);
        if (s.Tags is { Length: > 0 } tags)
        {
            w.WritePropertyName("tags");
            w.WriteStartArray();
            foreach (var t in tags) WriteKeyValue(w, t);
            w.WriteEndArray();
        }
        if (s.Events is { Length: > 0 } events)
        {
            w.WritePropertyName("events");
            w.WriteStartArray();
            foreach (var e in events)
            {
                w.WriteStartObject();
                w.WriteString("name", e.Name);
                w.WriteNumber("timestampMs", e.TimestampMs);
                if (e.Tags is { Length: > 0 } eTags)
                {
                    w.WritePropertyName("tags");
                    w.WriteStartArray();
                    foreach (var t in eTags) WriteKeyValue(w, t);
                    w.WriteEndArray();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        if (s.Links is { Length: > 0 } links)
        {
            w.WritePropertyName("links");
            w.WriteStartArray();
            foreach (var l in links)
            {
                w.WriteStartObject();
                w.WriteString("traceId", l.TraceId);
                w.WriteString("spanId", l.SpanId);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    private static void WriteKeyValue(Utf8JsonWriter w, ReportKeyValue kv)
    {
        w.WriteStartObject();
        w.WriteString("key", kv.Key);
        w.WriteString("value", kv.Value);
        w.WriteEndObject();
    }

    /// <summary>
    /// Reads a sidecar back into <see cref="ReportData"/>. Returns <see langword="null"/>
    /// (never throws) for malformed JSON, a missing/newer <c>schemaVersion</c>, or a file
    /// that isn't a TUnit report sidecar — a torn or foreign file in the aggregation
    /// directory must not take down every sibling process's merge.
    /// </summary>
    internal static ReportData? TryDeserialize(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("schemaVersion", out var version)
                || version.ValueKind != JsonValueKind.Number
                || version.GetInt32() > SchemaVersion)
            {
                return null;
            }

            return new ReportData
            {
                AssemblyName = GetString(root, "assemblyName") ?? "Unknown",
                MachineName = GetString(root, "machineName") ?? "",
                Timestamp = GetString(root, "timestamp") ?? "",
                TUnitVersion = GetString(root, "tunitVersion") ?? "",
                OperatingSystem = GetString(root, "operatingSystem") ?? "",
                RuntimeVersion = GetString(root, "runtimeVersion") ?? "",
                Filter = GetString(root, "filter"),
                TotalDurationMs = GetDouble(root, "totalDurationMs"),
                ArtifactUrl = GetString(root, "artifactUrl"),
                CommitSha = GetString(root, "commitSha"),
                Branch = GetString(root, "branch"),
                PullRequestNumber = GetString(root, "pullRequestNumber"),
                RepositorySlug = GetString(root, "repositorySlug"),
                SourceLinks = root.TryGetProperty("sourceLinks", out var sourceLinks)
                              && sourceLinks.ValueKind == JsonValueKind.Object
                              && GetString(sourceLinks, "lineUrl") is { } lineUrl
                              && GetString(sourceLinks, "rangeUrl") is { } rangeUrl
                    ? new SourceLinkTemplates(lineUrl, rangeUrl, GetString(sourceLinks, "rawUrl"))
                    : null,
                Summary = root.TryGetProperty("summary", out var summary) ? ReadSummary(summary) : new ReportSummary(),
                Groups = root.TryGetProperty("groups", out var groups) && groups.ValueKind == JsonValueKind.Array
                    ? ReadGroups(groups)
                    : [],
                Spans = root.TryGetProperty("spans", out var spans) && spans.ValueKind == JsonValueKind.Array
                    ? ReadSpans(spans)
                    : null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ReportSummary ReadSummary(JsonElement e) => new()
    {
        Total = GetInt(e, "total"),
        Passed = GetInt(e, "passed"),
        Failed = GetInt(e, "failed"),
        Skipped = GetInt(e, "skipped"),
        Cancelled = GetInt(e, "cancelled"),
        TimedOut = GetInt(e, "timedOut"),
        Flaky = GetInt(e, "flaky"),
    };

    private static ReportTestGroup[] ReadGroups(JsonElement array)
    {
        var groups = new ReportTestGroup[array.GetArrayLength()];
        var i = 0;
        foreach (var g in array.EnumerateArray())
        {
            groups[i++] = new ReportTestGroup
            {
                ClassName = GetString(g, "className") ?? "UnknownClass",
                Namespace = GetString(g, "namespace") ?? "",
                Summary = g.TryGetProperty("summary", out var summary) ? ReadSummary(summary) : new ReportSummary(),
                Tests = g.TryGetProperty("tests", out var tests) && tests.ValueKind == JsonValueKind.Array
                    ? ReadTests(tests)
                    : [],
            };
        }
        return groups;
    }

    private static ReportTestResult[] ReadTests(JsonElement array)
    {
        var tests = new ReportTestResult[array.GetArrayLength()];
        var i = 0;
        foreach (var t in array.EnumerateArray())
        {
            tests[i++] = new ReportTestResult
            {
                Id = GetString(t, "id") ?? "",
                DisplayName = GetString(t, "displayName") ?? "",
                MethodName = GetString(t, "methodName") ?? "",
                ClassName = GetString(t, "className") ?? "UnknownClass",
                Status = GetString(t, "status") ?? "unknown",
                DurationMs = GetDouble(t, "durationMs"),
                StartTime = GetString(t, "startTime"),
                EndTime = GetString(t, "endTime"),
                Exception = t.TryGetProperty("exception", out var ex) && ex.ValueKind == JsonValueKind.Object
                    ? ReadException(ex)
                    : null,
                Output = GetString(t, "output"),
                ErrorOutput = GetString(t, "errorOutput"),
                Categories = ReadStringArray(t, "categories"),
                CustomProperties = ReadKeyValues(t, "customProperties"),
                FilePath = GetString(t, "filePath"),
                LineNumber = GetNullableInt(t, "lineNumber"),
                EndLineNumber = GetNullableInt(t, "endLineNumber"),
                SourceRelativePath = GetString(t, "sourceRelativePath"),
                SkipReason = GetString(t, "skipReason"),
                RetryAttempt = GetInt(t, "retryAttempt"),
                Attempts = ReadAttempts(t),
                TraceId = GetString(t, "traceId"),
                SpanId = GetString(t, "spanId"),
                AdditionalTraceIds = ReadStringArray(t, "additionalTraceIds"),
            };
        }
        return tests;
    }

    private static ReportAttempt[]? ReadAttempts(JsonElement test)
    {
        if (!test.TryGetProperty("attempts", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var attempts = new ReportAttempt[array.GetArrayLength()];
        var i = 0;
        foreach (var a in array.EnumerateArray())
        {
            attempts[i++] = new ReportAttempt
            {
                Status = GetString(a, "status") ?? "unknown",
                DurationMs = GetDouble(a, "durationMs"),
                ExceptionType = GetString(a, "exceptionType"),
                ExceptionMessage = GetString(a, "exceptionMessage"),
                StackTrace = GetString(a, "stackTrace"),
            };
        }
        return attempts;
    }

    private static ReportExceptionData ReadException(JsonElement e) => new()
    {
        Type = GetString(e, "type") ?? "Unknown",
        Message = GetString(e, "message") ?? "",
        StackTrace = GetString(e, "stackTrace"),
        InnerException = e.TryGetProperty("innerException", out var inner) && inner.ValueKind == JsonValueKind.Object
            ? ReadException(inner)
            : null,
    };

    private static SpanData[] ReadSpans(JsonElement array)
    {
        var spans = new SpanData[array.GetArrayLength()];
        var i = 0;
        foreach (var s in array.EnumerateArray())
        {
            spans[i++] = new SpanData
            {
                TraceId = GetString(s, "traceId") ?? "",
                SpanId = GetString(s, "spanId") ?? "",
                ParentSpanId = GetString(s, "parentSpanId"),
                Name = GetString(s, "name") ?? "",
                SpanType = GetString(s, "spanType"),
                Source = GetString(s, "source") ?? "",
                Kind = GetString(s, "kind") ?? "",
                StartTimeMs = GetDouble(s, "startTimeMs"),
                DurationMs = GetDouble(s, "durationMs"),
                Status = GetString(s, "status") ?? "Unset",
                StatusMessage = GetString(s, "statusMessage"),
                Tags = ReadKeyValues(s, "tags"),
                Events = ReadEvents(s),
                Links = ReadLinks(s),
            };
        }
        return spans;
    }

    private static SpanEvent[]? ReadEvents(JsonElement span)
    {
        if (!span.TryGetProperty("events", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var events = new SpanEvent[array.GetArrayLength()];
        var i = 0;
        foreach (var e in array.EnumerateArray())
        {
            events[i++] = new SpanEvent
            {
                Name = GetString(e, "name") ?? "",
                TimestampMs = GetDouble(e, "timestampMs"),
                Tags = ReadKeyValues(e, "tags"),
            };
        }
        return events;
    }

    private static SpanLink[]? ReadLinks(JsonElement span)
    {
        if (!span.TryGetProperty("links", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var links = new SpanLink[array.GetArrayLength()];
        var i = 0;
        foreach (var l in array.EnumerateArray())
        {
            links[i++] = new SpanLink
            {
                TraceId = GetString(l, "traceId") ?? "",
                SpanId = GetString(l, "spanId") ?? "",
            };
        }
        return links;
    }

    private static ReportKeyValue[]? ReadKeyValues(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new ReportKeyValue[array.GetArrayLength()];
        var i = 0;
        foreach (var kv in array.EnumerateArray())
        {
            values[i++] = new ReportKeyValue
            {
                Key = GetString(kv, "key") ?? "",
                Value = GetString(kv, "value") ?? "",
            };
        }
        return values;
    }

    private static string[]? ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new string[array.GetArrayLength()];
        var i = 0;
        foreach (var v in array.EnumerateArray())
        {
            values[i++] = v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        }
        return values;
    }

    private static string? GetString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double GetDouble(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static int GetInt(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static int? GetNullableInt(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
}
