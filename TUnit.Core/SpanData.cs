using System.Text.Json.Serialization;

namespace TUnit.Core;

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

    [JsonPropertyName("spanType")]
    public string? SpanType { get; init; }

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

    [JsonPropertyName("links")]
    public SpanLink[]? Links { get; init; }
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

internal sealed class SpanLink
{
    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }

    [JsonPropertyName("spanId")]
    public required string SpanId { get; init; }
}

internal sealed class ReportKeyValue
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
