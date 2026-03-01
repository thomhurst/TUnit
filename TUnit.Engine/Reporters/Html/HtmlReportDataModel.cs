using System.Text.Json.Serialization;

namespace TUnit.Engine.Reporters.Html;

internal sealed class ReportData
{
    [JsonPropertyName("assemblyName")]
    public required string AssemblyName { get; init; }

    [JsonPropertyName("machineName")]
    public required string MachineName { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("tunitVersion")]
    public required string TUnitVersion { get; init; }

    [JsonPropertyName("operatingSystem")]
    public required string OperatingSystem { get; init; }

    [JsonPropertyName("runtimeVersion")]
    public required string RuntimeVersion { get; init; }

    [JsonPropertyName("filter")]
    public string? Filter { get; init; }

    [JsonPropertyName("totalDurationMs")]
    public double TotalDurationMs { get; init; }

    [JsonPropertyName("summary")]
    public required ReportSummary Summary { get; init; }

    [JsonPropertyName("groups")]
    public required ReportTestGroup[] Groups { get; init; }

    [JsonPropertyName("spans")]
    public SpanData[]? Spans { get; init; }

    [JsonPropertyName("commitSha")]
    public string? CommitSha { get; init; }

    [JsonPropertyName("branch")]
    public string? Branch { get; init; }

    [JsonPropertyName("pullRequestNumber")]
    public string? PullRequestNumber { get; init; }

    [JsonPropertyName("repositorySlug")]
    public string? RepositorySlug { get; init; }
}

internal sealed class ReportSummary
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }

    [JsonPropertyName("timedOut")]
    public int TimedOut { get; set; }
}

internal sealed class ReportTestGroup
{
    [JsonPropertyName("className")]
    public required string ClassName { get; init; }

    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    [JsonPropertyName("summary")]
    public required ReportSummary Summary { get; init; }

    [JsonPropertyName("tests")]
    public required ReportTestResult[] Tests { get; init; }
}

internal sealed class ReportTestResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("methodName")]
    public required string MethodName { get; init; }

    [JsonPropertyName("className")]
    public required string ClassName { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("durationMs")]
    public double DurationMs { get; init; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; init; }

    [JsonPropertyName("exception")]
    public ReportExceptionData? Exception { get; init; }

    [JsonPropertyName("output")]
    public string? Output { get; init; }

    [JsonPropertyName("errorOutput")]
    public string? ErrorOutput { get; init; }

    [JsonPropertyName("categories")]
    public string[]? Categories { get; init; }

    [JsonPropertyName("customProperties")]
    public ReportKeyValue[]? CustomProperties { get; init; }

    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("lineNumber")]
    public int? LineNumber { get; init; }

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; init; }

    [JsonPropertyName("retryAttempt")]
    public int RetryAttempt { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("spanId")]
    public string? SpanId { get; init; }
}

internal sealed class ReportExceptionData
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("innerException")]
    public ReportExceptionData? InnerException { get; init; }
}

internal sealed class ReportKeyValue
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

internal sealed class SpanData
{
    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }

    [JsonPropertyName("spanId")]
    public required string SpanId { get; init; }

    [JsonPropertyName("parentSpanId")]
    public string? ParentSpanId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("startTimeMs")]
    public double StartTimeMs { get; init; }

    [JsonPropertyName("durationMs")]
    public double DurationMs { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; init; }

    [JsonPropertyName("tags")]
    public ReportKeyValue[]? Tags { get; init; }

    [JsonPropertyName("events")]
    public SpanEvent[]? Events { get; init; }
}

internal sealed class SpanEvent
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("timestampMs")]
    public double TimestampMs { get; init; }

    [JsonPropertyName("tags")]
    public ReportKeyValue[]? Tags { get; init; }
}
