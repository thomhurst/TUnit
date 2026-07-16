namespace TUnit.Aspire.Tests.Helpers;

/// <summary>
/// Builds OTLP ExportLogsServiceRequest protobuf binary messages for testing.
/// Mirrors the OTLP proto schema without requiring a protobuf library.
/// </summary>
internal static class OtlpProtobufBuilder
{
    /// <summary>
    /// Builds a complete ExportLogsServiceRequest containing one ResourceLogs
    /// with one ScopeLogs containing the provided log records.
    /// </summary>
    public static byte[] BuildExportLogsServiceRequest(
        string? serviceName,
        params LogRecordSpec[] logRecords)
    {
        return BuildExportLogsServiceRequest([(serviceName, logRecords)]);
    }

    /// <summary>
    /// Builds an ExportLogsServiceRequest with multiple ResourceLogs,
    /// each with its own service name and log records.
    /// </summary>
    public static byte[] BuildExportLogsServiceRequest(
        params (string? ServiceName, LogRecordSpec[] Records)[] resourceLogs)
    {
        using var ms = new MemoryStream();

        foreach (var (serviceName, records) in resourceLogs)
        {
            var resourceLogsBytes = BuildResourceLogs(serviceName, records);
            // Field 1, wire type 2 (length-delimited)
            WriteTag(ms, 1, 2);
            WriteLengthDelimited(ms, resourceLogsBytes);
        }

        return ms.ToArray();
    }

    private static byte[] BuildResourceLogs(string? serviceName, LogRecordSpec[] records)
    {
        using var ms = new MemoryStream();

        // Field 1: Resource
        if (serviceName is not null)
        {
            var resourceBytes = BuildResource(serviceName);
            WriteTag(ms, 1, 2);
            WriteLengthDelimited(ms, resourceBytes);
        }

        // Field 2: ScopeLogs
        var scopeLogsBytes = BuildScopeLogs(records);
        WriteTag(ms, 2, 2);
        WriteLengthDelimited(ms, scopeLogsBytes);

        return ms.ToArray();
    }

    private static byte[] BuildResource(string serviceName)
    {
        using var ms = new MemoryStream();

        // Field 1: repeated KeyValue attributes
        var kvBytes = BuildKeyValue("service.name", serviceName);
        WriteTag(ms, 1, 2);
        WriteLengthDelimited(ms, kvBytes);

        return ms.ToArray();
    }

    private static byte[] BuildKeyValue(string key, string value)
    {
        using var ms = new MemoryStream();

        // Field 1: string key
        WriteTag(ms, 1, 2);
        WriteString(ms, key);

        // Field 2: AnyValue value
        var anyValueBytes = BuildAnyValueString(value);
        WriteTag(ms, 2, 2);
        WriteLengthDelimited(ms, anyValueBytes);

        return ms.ToArray();
    }

    private static byte[] BuildAnyValueString(string value)
    {
        using var ms = new MemoryStream();

        // Field 1: string string_value
        WriteTag(ms, 1, 2);
        WriteString(ms, value);

        return ms.ToArray();
    }

    private static byte[] BuildScopeLogs(LogRecordSpec[] records)
    {
        using var ms = new MemoryStream();

        // Field 2: repeated LogRecord log_records
        foreach (var record in records)
        {
            var logRecordBytes = BuildLogRecord(record);
            WriteTag(ms, 2, 2);
            WriteLengthDelimited(ms, logRecordBytes);
        }

        return ms.ToArray();
    }

    private static byte[] BuildLogRecord(LogRecordSpec spec)
    {
        using var ms = new MemoryStream();

        // Field 2: SeverityNumber (varint)
        if (spec.SeverityNumber > 0)
        {
            WriteTag(ms, 2, 0);
            WriteVarint(ms, (ulong)spec.SeverityNumber);
        }

        // Field 3: severity_text (string)
        if (spec.SeverityText is not null)
        {
            WriteTag(ms, 3, 2);
            WriteString(ms, spec.SeverityText);
        }

        // Field 5: body (AnyValue)
        if (spec.Body is not null)
        {
            var bodyBytes = BuildAnyValueString(spec.Body);
            WriteTag(ms, 5, 2);
            WriteLengthDelimited(ms, bodyBytes);
        }

        // Field 9: trace_id (bytes, 16 bytes)
        if (spec.TraceId is not null)
        {
            var traceIdBytes = Convert.FromHexString(spec.TraceId);
            WriteTag(ms, 9, 2);
            WriteLengthDelimited(ms, traceIdBytes);
        }

        // Field 10: span_id (bytes, 8 bytes) — included for realism
        if (spec.SpanId is not null)
        {
            var spanIdBytes = Convert.FromHexString(spec.SpanId);
            WriteTag(ms, 10, 2);
            WriteLengthDelimited(ms, spanIdBytes);
        }

        return ms.ToArray();
    }

    // --- Wire format helpers ---

    private static void WriteTag(MemoryStream ms, int fieldNumber, int wireType)
    {
        WriteVarint(ms, (ulong)((fieldNumber << 3) | wireType));
    }

    private static void WriteVarint(MemoryStream ms, ulong value)
    {
        do
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                b |= 0x80;
            }

            ms.WriteByte(b);
        } while (value != 0);
    }

    private static void WriteLengthDelimited(MemoryStream ms, byte[] data)
    {
        WriteVarint(ms, (ulong)data.Length);
        ms.Write(data, 0, data.Length);
    }

    private static void WriteString(MemoryStream ms, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteLengthDelimited(ms, bytes);
    }
}

/// <summary>
/// Specifies the fields for a single log record in the protobuf builder.
/// </summary>
internal record LogRecordSpec
{
    /// <summary>32-character hex trace ID (e.g. "0123456789abcdef0123456789abcdef").</summary>
    public string? TraceId { get; init; }

    /// <summary>16-character hex span ID.</summary>
    public string? SpanId { get; init; }

    /// <summary>OTLP severity number (1-24).</summary>
    public int SeverityNumber { get; init; }

    /// <summary>Severity text (e.g. "INFO", "ERROR").</summary>
    public string? SeverityText { get; init; }

    /// <summary>Log body text.</summary>
    public string? Body { get; init; }
}
