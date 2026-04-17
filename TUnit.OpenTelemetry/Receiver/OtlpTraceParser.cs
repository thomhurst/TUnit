namespace TUnit.OpenTelemetry.Receiver;

/// <summary>
/// A parsed OTLP span record. Only the fields needed to render a TUnit HTML report
/// span row are extracted; dropped_*_count fields are ignored.
/// </summary>
internal sealed record OtlpSpanRecord(
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    int Kind,
    long StartTimeUnixNano,
    long EndTimeUnixNano,
    int StatusCode,
    string StatusMessage,
    IReadOnlyList<KeyValuePair<string, string>> Attributes,
    IReadOnlyList<OtlpSpanEvent> Events,
    IReadOnlyList<OtlpSpanLink> Links,
    string ResourceName,
    string ScopeName);

internal readonly record struct OtlpSpanEvent(
    long TimeUnixNano,
    string Name,
    IReadOnlyList<KeyValuePair<string, string>> Attributes);

internal readonly record struct OtlpSpanLink(
    string TraceId,
    string SpanId);

/// <summary>
/// Minimal parser for OTLP ExportTraceServiceRequest protobuf messages.
/// Extracts only the fields needed to forward spans into TUnit's HTML report.
/// Uses the same hand-rolled <see cref="ProtobufReader"/> as <see cref="OtlpLogParser"/> —
/// no external protobuf dependency.
/// </summary>
internal static class OtlpTraceParser
{
    public static IReadOnlyList<OtlpSpanRecord> Parse(ReadOnlySpan<byte> data)
    {
        var results = new List<OtlpSpanRecord>();
        var reader = new ProtobufReader(data);

        // ExportTraceServiceRequest: field 1 = repeated ResourceSpans
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                ParseResourceSpans(embedded, results);
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return results;
    }

    private static void ParseResourceSpans(ProtobufReader reader, List<OtlpSpanRecord> results)
    {
        var resourceName = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                resourceName = ParseResourceServiceName(embedded);
            }
            else if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                ParseScopeSpans(embedded, resourceName, results);
            }
            else
            {
                reader.Skip(wireType);
            }
        }
    }

    private static string ParseResourceServiceName(ProtobufReader reader)
    {
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                var (key, value) = ParseKeyValue(embedded);
                if (key == "service.name")
                {
                    return value;
                }
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return "";
    }

    private static void ParseScopeSpans(
        ProtobufReader reader,
        string resourceName,
        List<OtlpSpanRecord> results)
    {
        var scopeName = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                scopeName = ParseInstrumentationScopeName(embedded);
            }
            else if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                var span = ParseSpan(embedded, resourceName, scopeName);
                if (span is not null)
                {
                    results.Add(span);
                }
            }
            else
            {
                reader.Skip(wireType);
            }
        }
    }

    private static string ParseInstrumentationScopeName(ProtobufReader reader)
    {
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                return reader.ReadString();
            }

            reader.Skip(wireType);
        }

        return "";
    }

    private static OtlpSpanRecord? ParseSpan(ProtobufReader reader, string resourceName, string scopeName)
    {
        var traceId = "";
        var spanId = "";
        string? parentSpanId = null;
        var name = "";
        var kind = 0;
        long startTimeUnixNano = 0;
        long endTimeUnixNano = 0;
        var statusCode = 0;
        var statusMessage = "";
        List<KeyValuePair<string, string>>? attributes = null;
        List<OtlpSpanEvent>? events = null;
        List<OtlpSpanLink>? links = null;

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 1 when wireType == WireType.LengthDelimited:
                    var traceBytes = reader.ReadBytesAsSpan();
                    if (traceBytes.Length == 16)
                    {
                        traceId = Convert.ToHexString(traceBytes);
                    }

                    break;

                case 2 when wireType == WireType.LengthDelimited:
                    var spanBytes = reader.ReadBytesAsSpan();
                    if (spanBytes.Length == 8)
                    {
                        spanId = Convert.ToHexString(spanBytes);
                    }

                    break;

                case 4 when wireType == WireType.LengthDelimited:
                    var parentBytes = reader.ReadBytesAsSpan();
                    if (parentBytes.Length == 8)
                    {
                        parentSpanId = Convert.ToHexString(parentBytes);
                    }

                    break;

                case 5 when wireType == WireType.LengthDelimited:
                    name = reader.ReadString();
                    break;

                case 6 when wireType == WireType.Varint:
                    kind = (int)reader.ReadVarint();
                    break;

                case 7 when wireType == WireType.Fixed64:
                    startTimeUnixNano = reader.ReadFixed64();
                    break;

                case 8 when wireType == WireType.Fixed64:
                    endTimeUnixNano = reader.ReadFixed64();
                    break;

                case 9 when wireType == WireType.LengthDelimited:
                    attributes ??= new List<KeyValuePair<string, string>>();
                    var attr = reader.ReadEmbeddedMessage();
                    var (attrKey, attrValue) = ParseKeyValue(attr);
                    attributes.Add(new KeyValuePair<string, string>(attrKey, attrValue));
                    break;

                case 11 when wireType == WireType.LengthDelimited:
                    events ??= new List<OtlpSpanEvent>();
                    var eventMsg = reader.ReadEmbeddedMessage();
                    events.Add(ParseSpanEvent(eventMsg));
                    break;

                case 13 when wireType == WireType.LengthDelimited:
                    links ??= new List<OtlpSpanLink>();
                    var linkMsg = reader.ReadEmbeddedMessage();
                    var link = ParseSpanLink(linkMsg);
                    if (link is not null)
                    {
                        links.Add(link.Value);
                    }

                    break;

                case 15 when wireType == WireType.LengthDelimited:
                    var statusMsg = reader.ReadEmbeddedMessage();
                    (statusCode, statusMessage) = ParseStatus(statusMsg);
                    break;

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        if (string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(spanId))
        {
            return null;
        }

        return new OtlpSpanRecord(
            traceId,
            spanId,
            parentSpanId,
            name,
            kind,
            startTimeUnixNano,
            endTimeUnixNano,
            statusCode,
            statusMessage,
            (IReadOnlyList<KeyValuePair<string, string>>?)attributes ?? Array.Empty<KeyValuePair<string, string>>(),
            (IReadOnlyList<OtlpSpanEvent>?)events ?? Array.Empty<OtlpSpanEvent>(),
            (IReadOnlyList<OtlpSpanLink>?)links ?? Array.Empty<OtlpSpanLink>(),
            resourceName,
            scopeName);
    }

    private static OtlpSpanEvent ParseSpanEvent(ProtobufReader reader)
    {
        long timeUnixNano = 0;
        var name = "";
        List<KeyValuePair<string, string>>? attributes = null;

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 1 when wireType == WireType.Fixed64:
                    timeUnixNano = reader.ReadFixed64();
                    break;

                case 2 when wireType == WireType.LengthDelimited:
                    name = reader.ReadString();
                    break;

                case 3 when wireType == WireType.LengthDelimited:
                    attributes ??= new List<KeyValuePair<string, string>>();
                    var attr = reader.ReadEmbeddedMessage();
                    var (k, v) = ParseKeyValue(attr);
                    attributes.Add(new KeyValuePair<string, string>(k, v));
                    break;

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        return new OtlpSpanEvent(
            timeUnixNano,
            name,
            (IReadOnlyList<KeyValuePair<string, string>>?)attributes
                ?? Array.Empty<KeyValuePair<string, string>>());
    }

    private static OtlpSpanLink? ParseSpanLink(ProtobufReader reader)
    {
        var traceId = "";
        var spanId = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 1 when wireType == WireType.LengthDelimited:
                    var traceBytes = reader.ReadBytesAsSpan();
                    if (traceBytes.Length == 16)
                    {
                        traceId = Convert.ToHexString(traceBytes);
                    }

                    break;

                case 2 when wireType == WireType.LengthDelimited:
                    var spanBytes = reader.ReadBytesAsSpan();
                    if (spanBytes.Length == 8)
                    {
                        spanId = Convert.ToHexString(spanBytes);
                    }

                    break;

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        if (string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(spanId))
        {
            return null;
        }

        return new OtlpSpanLink(traceId, spanId);
    }

    private static (int Code, string Message) ParseStatus(ProtobufReader reader)
    {
        var code = 0;
        var message = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 2 when wireType == WireType.LengthDelimited:
                    message = reader.ReadString();
                    break;

                case 3 when wireType == WireType.Varint:
                    code = (int)reader.ReadVarint();
                    break;

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        return (code, message);
    }

    private static (string Key, string Value) ParseKeyValue(ProtobufReader reader)
    {
        var key = "";
        var value = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                key = reader.ReadString();
            }
            else if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                value = ParseAnyValue(embedded);
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return (key, value);
    }

    private static string ParseAnyValue(ProtobufReader reader)
    {
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 1 when wireType == WireType.LengthDelimited:
                    return reader.ReadString();

                case 2 when wireType == WireType.Varint:
                    return reader.ReadVarint() != 0 ? "true" : "false";

                case 3 when wireType == WireType.Varint:
                    return ((long)reader.ReadVarint()).ToString(System.Globalization.CultureInfo.InvariantCulture);

                case 4 when wireType == WireType.Fixed64:
                    var bits = reader.ReadFixed64();
                    return BitConverter.Int64BitsToDouble(bits)
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        return "";
    }
}
