using System.Text;

namespace TUnit.OpenTelemetry.Receiver;

/// <summary>
/// A parsed OTLP log record containing only the fields needed for test correlation.
/// </summary>
/// <param name="TraceId">The W3C trace ID (32 hex chars).</param>
/// <param name="SeverityText">The severity text (e.g., "INFO", "ERROR").</param>
/// <param name="SeverityNumber">The OTLP severity number (1-24).</param>
/// <param name="Body">
/// Body of the log record. Only populated when the OTLP body is a string value;
/// other value types (int, bool, kvlist, array) are not currently extracted.
/// </param>
/// <param name="ResourceName">The <c>service.name</c> resource attribute, if present.</param>
/// <param name="ExceptionType">
/// The <c>exception.type</c> log attribute, if present. Populated by the OTLP log exporter
/// (OpenTelemetry .NET 1.8.0+) whenever a log record carries an exception. Empty otherwise.
/// </param>
/// <param name="ExceptionMessage">The <c>exception.message</c> log attribute, if present. Empty otherwise.</param>
/// <param name="ExceptionStackTrace">
/// The <c>exception.stacktrace</c> log attribute, if present. In OpenTelemetry .NET this is the
/// full <c>Exception.ToString()</c> (type, message, and stack), so it already subsumes the type
/// and message fields. Empty otherwise.
/// </param>
internal readonly record struct OtlpLogRecord(
    string TraceId,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string ResourceName,
    string ExceptionType = "",
    string ExceptionMessage = "",
    string ExceptionStackTrace = "")
{
    /// <summary>
    /// Renders the exception attributes into a single human-readable block, or <c>null</c> when the
    /// record carries no exception. Prefers <see cref="ExceptionStackTrace"/> (the full
    /// <c>ToString()</c>); otherwise falls back to <c>type: message</c> from the discrete fields.
    /// </summary>
    public string? FormatException()
    {
        if (!string.IsNullOrEmpty(ExceptionStackTrace))
        {
            return ExceptionStackTrace;
        }

        if (!string.IsNullOrEmpty(ExceptionType) && !string.IsNullOrEmpty(ExceptionMessage))
        {
            return $"{ExceptionType}: {ExceptionMessage}";
        }

        if (!string.IsNullOrEmpty(ExceptionType))
        {
            return ExceptionType;
        }

        return string.IsNullOrEmpty(ExceptionMessage) ? null : ExceptionMessage;
    }
}

/// <summary>
/// Minimal parser for OTLP ExportLogsServiceRequest protobuf messages.
/// Extracts only the fields needed for test correlation (TraceId, severity, body)
/// without requiring any external protobuf library.
///
/// Field numbers below are from the OTLP proto definitions:
///   ExportLogsServiceRequest: https://github.com/open-telemetry/opentelemetry-proto/blob/main/opentelemetry/proto/collector/logs/v1/logs_service.proto
///   ResourceLogs/ScopeLogs/LogRecord: https://github.com/open-telemetry/opentelemetry-proto/blob/main/opentelemetry/proto/logs/v1/logs.proto
///   Resource/KeyValue/AnyValue: https://github.com/open-telemetry/opentelemetry-proto/blob/main/opentelemetry/proto/common/v1/common.proto
/// </summary>
internal static class OtlpLogParser
{
    /// <param name="onResourceSeen">
    /// Invoked with each non-empty <c>service.name</c> that produced a log record, <em>before</em>
    /// the trace-id filter drops untraced records. Lets a consumer record that OTLP logging reached
    /// it at all — independent of whether any record carried a usable trace id (records without one
    /// are still excluded from the returned list, per the correlation contract).
    /// </param>
    public static List<OtlpLogRecord> Parse(ReadOnlySpan<byte> data, Action<string>? onResourceSeen = null)
    {
        var results = new List<OtlpLogRecord>();
        var reader = new ProtobufReader(data);

        // ExportLogsServiceRequest: field 1 = repeated ResourceLogs
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                ParseResourceLogs(embedded, results, onResourceSeen);
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return results;
    }

    // Assumes Resource (field 1) precedes ScopeLogs (field 2) in the wire format,
    // which is true for all known OTel SDK implementations.
    private static void ParseResourceLogs(ProtobufReader reader, List<OtlpLogRecord> results, Action<string>? onResourceSeen)
    {
        var resourceName = "";
        var sawLogRecord = false;

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                // Resource message — extract service.name
                var embedded = reader.ReadEmbeddedMessage();
                resourceName = ParseResourceServiceName(embedded);
            }
            else if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
                // ScopeLogs
                var embedded = reader.ReadEmbeddedMessage();
                sawLogRecord |= ParseScopeLogs(embedded, resourceName, results);
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        // Signal the source service once — independent of the trace-id filter that drops untraced
        // records from `results` — as long as it emitted at least one log record. Presence (not
        // correlation) is what the missing-telemetry hint checks for. Done once per resource rather
        // than per record to avoid redundant work for a noisy resource.
        if (sawLogRecord && !string.IsNullOrEmpty(resourceName))
        {
            onResourceSeen?.Invoke(resourceName);
        }
    }

    private static string ParseResourceServiceName(ProtobufReader reader)
    {
        // Resource: field 1 = repeated KeyValue attributes
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

    // Returns whether any LogRecord was present, so the caller can signal the source service once
    // per resource (even when every record is dropped by the trace-id filter).
    private static bool ParseScopeLogs(ProtobufReader reader, string resourceName, List<OtlpLogRecord> results)
    {
        var sawLogRecord = false;

        // ScopeLogs: field 2 = repeated LogRecord
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
                sawLogRecord = true;
                var embedded = reader.ReadEmbeddedMessage();
                var record = ParseLogRecord(embedded, resourceName);
                if (record is not null)
                {
                    results.Add(record.Value);
                }
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return sawLogRecord;
    }

    private static OtlpLogRecord? ParseLogRecord(ProtobufReader reader, string resourceName)
    {
        var traceId = "";
        var severityNumber = 0;
        var severityText = "";
        var body = "";
        var exceptionType = "";
        var exceptionMessage = "";
        var exceptionStackTrace = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber)
            {
                case 2 when wireType == WireType.Varint:
                    severityNumber = (int)reader.ReadVarint();
                    break;

                case 3 when wireType == WireType.LengthDelimited:
                    severityText = reader.ReadString();
                    break;

                case 5 when wireType == WireType.LengthDelimited:
                    var bodyMsg = reader.ReadEmbeddedMessage();
                    body = ParseAnyValueString(bodyMsg);
                    break;

                // LogRecord.attributes (field 6) — OpenTelemetry's OTLP log exporter attaches the
                // exception.* semantic-convention attributes here whenever a record carries an
                // exception. Pull those three out so the exception can be surfaced alongside the body
                // (the body alone is often just the log message, not the failure detail).
                case 6 when wireType == WireType.LengthDelimited:
                    var (key, value) = ParseExceptionAttribute(reader.ReadEmbeddedMessage());
                    switch (key)
                    {
                        case "exception.type":
                            exceptionType = value;
                            break;
                        case "exception.message":
                            exceptionMessage = value;
                            break;
                        case "exception.stacktrace":
                            exceptionStackTrace = value;
                            break;
                    }

                    break;

                case 9 when wireType == WireType.LengthDelimited:
                    var traceBytes = reader.ReadBytesAsSpan();
                    if (traceBytes.Length == 16)
                    {
                        traceId = Convert.ToHexString(traceBytes);
                    }

                    break;

                default:
                    reader.Skip(wireType);
                    break;
            }
        }

        if (string.IsNullOrEmpty(traceId))
        {
            return null;
        }

        return new OtlpLogRecord(
            traceId,
            severityText,
            severityNumber,
            body,
            resourceName,
            exceptionType,
            exceptionMessage,
            exceptionStackTrace);
    }

    private static string ParseAnyValueString(ProtobufReader reader)
    {
        // AnyValue: field 1 = string_value
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

    // Like ParseKeyValue, but materialises the value string only for the exception.* keys the
    // receiver renders. A log record can carry many attributes (scopes, custom fields); parsing
    // every value — some large — just to discard it would waste allocations on the ingest hot
    // path. Assumes key (field 1) precedes value (field 2), which holds for all known OTel encoders.
    private static (string Key, string Value) ParseExceptionAttribute(ProtobufReader reader)
    {
        var key = "";

        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                key = reader.ReadString();
            }
            else if (fieldNumber == 2 && wireType == WireType.LengthDelimited
                && key is "exception.type" or "exception.message" or "exception.stacktrace")
            {
                return (key, ParseAnyValueString(reader.ReadEmbeddedMessage()));
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return (key, "");
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
                value = ParseAnyValueString(embedded);
            }
            else
            {
                reader.Skip(wireType);
            }
        }

        return (key, value);
    }

    internal static string FormatSeverity(int severityNumber, string severityText)
    {
        if (!string.IsNullOrEmpty(severityText))
        {
            return severityText;
        }

        return severityNumber switch
        {
            >= 1 and <= 4 => "TRACE",
            >= 5 and <= 8 => "DEBUG",
            >= 9 and <= 12 => "INFO",
            >= 13 and <= 16 => "WARN",
            >= 17 and <= 20 => "ERROR",
            >= 21 and <= 24 => "FATAL",
            _ => $"SEV{severityNumber}",
        };
    }
}

/// <summary>
/// Protobuf wire type values.
/// </summary>
internal enum WireType
{
    Varint = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    Fixed32 = 5,
}

/// <summary>
/// Minimal protobuf binary reader. Reads the wire format without requiring
/// generated message types or external libraries.
/// </summary>
internal ref struct ProtobufReader
{
    private ReadOnlySpan<byte> _data;

    public ProtobufReader(ReadOnlySpan<byte> data) => _data = data;

    public bool TryReadTag(out int fieldNumber, out WireType wireType)
    {
        if (_data.IsEmpty)
        {
            fieldNumber = 0;
            wireType = 0;
            return false;
        }

        var tag = ReadVarint();
        fieldNumber = (int)(tag >> 3);
        wireType = (WireType)(tag & 0x07);
        return true;
    }

    public ulong ReadVarint()
    {
        ulong result = 0;
        var shift = 0;

        while (!_data.IsEmpty)
        {
            if (shift >= 64)
            {
                throw new InvalidOperationException("Malformed varint: exceeds 64 bits.");
            }

            var b = _data[0];
            _data = _data[1..];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }

        return result;
    }

    public ReadOnlySpan<byte> ReadBytesAsSpan()
    {
        return ReadLengthDelimited();
    }

    public long ReadFixed64()
    {
        if (_data.Length < 8)
        {
            throw new InvalidOperationException(
                $"Truncated fixed64 field: need 8 bytes but only {_data.Length} remain.");
        }

        var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(_data);
        _data = _data[8..];
        return (long)result;
    }

    public string ReadString()
    {
        return Encoding.UTF8.GetString(ReadLengthDelimited());
    }

    public ProtobufReader ReadEmbeddedMessage()
    {
        return new ProtobufReader(ReadLengthDelimited());
    }

    private ReadOnlySpan<byte> ReadLengthDelimited()
    {
        var length = (int)ReadVarint();

        if ((uint)length > (uint)_data.Length)
        {
            throw new InvalidOperationException(
                $"Protobuf length-delimited field declares {length} bytes but only {_data.Length} remain.");
        }

        var result = _data[..length];
        _data = _data[length..];
        return result;
    }

    public void Skip(WireType wireType)
    {
        switch (wireType)
        {
            case WireType.Varint:
                ReadVarint();
                break;
            case WireType.Fixed64:
                if (_data.Length < 8)
                {
                    throw new InvalidOperationException(
                        $"Truncated fixed64 field: need 8 bytes but only {_data.Length} remain.");
                }

                _data = _data[8..];
                break;
            case WireType.LengthDelimited:
                ReadLengthDelimited();
                break;
            case WireType.Fixed32:
                if (_data.Length < 4)
                {
                    throw new InvalidOperationException(
                        $"Truncated fixed32 field: need 4 bytes but only {_data.Length} remain.");
                }

                _data = _data[4..];
                break;
            default:
                throw new InvalidOperationException($"Unknown protobuf wire type: {wireType}");
        }
    }
}
