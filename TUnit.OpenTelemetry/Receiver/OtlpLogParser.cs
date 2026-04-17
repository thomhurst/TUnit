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
internal readonly record struct OtlpLogRecord(
    string TraceId,
    string SeverityText,
    int SeverityNumber,
    string Body,
    string ResourceName);

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
    public static List<OtlpLogRecord> Parse(ReadOnlySpan<byte> data)
    {
        var results = new List<OtlpLogRecord>();
        var reader = new ProtobufReader(data);

        // ExportLogsServiceRequest: field 1 = repeated ResourceLogs
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 1 && wireType == WireType.LengthDelimited)
            {
                var embedded = reader.ReadEmbeddedMessage();
                ParseResourceLogs(embedded, results);
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
    private static void ParseResourceLogs(ProtobufReader reader, List<OtlpLogRecord> results)
    {
        var resourceName = "";

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
                ParseScopeLogs(embedded, resourceName, results);
            }
            else
            {
                reader.Skip(wireType);
            }
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

    private static void ParseScopeLogs(ProtobufReader reader, string resourceName, List<OtlpLogRecord> results)
    {
        // ScopeLogs: field 2 = repeated LogRecord
        while (reader.TryReadTag(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 2 && wireType == WireType.LengthDelimited)
            {
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
    }

    private static OtlpLogRecord? ParseLogRecord(ProtobufReader reader, string resourceName)
    {
        var traceId = "";
        var severityNumber = 0;
        var severityText = "";
        var body = "";

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

        return new OtlpLogRecord(traceId, severityText, severityNumber, body, resourceName);
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

        var result = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(_data);
        _data = _data[8..];
        return result;
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
