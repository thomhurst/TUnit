using TUnit.Aspire.Tests.Helpers;
using TUnit.OpenTelemetry.Receiver;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class OtlpLogParserTests
{
    [Test]
    public async Task Parse_SingleLogRecord_ExtractsAllFields()
    {
        var traceId = "0123456789abcdef0123456789abcdef";
        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec
            {
                TraceId = traceId,
                SeverityNumber = 9,
                SeverityText = "INFO",
                Body = "Request received",
            });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(1);
        await Assert.That(records[0].TraceId).IsEqualTo(traceId.ToUpperInvariant());
        await Assert.That(records[0].SeverityNumber).IsEqualTo(9);
        await Assert.That(records[0].SeverityText).IsEqualTo("INFO");
        await Assert.That(records[0].Body).IsEqualTo("Request received");
        await Assert.That(records[0].ResourceName).IsEqualTo("my-api");
    }

    [Test]
    public async Task Parse_MultipleLogRecords_ExtractsAll()
    {
        var traceId1 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1";
        var traceId2 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "worker-svc",
            new LogRecordSpec { TraceId = traceId1, SeverityNumber = 9, Body = "First log" },
            new LogRecordSpec { TraceId = traceId2, SeverityNumber = 17, Body = "Second log" });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(2);
        await Assert.That(records[0].TraceId).IsEqualTo(traceId1.ToUpperInvariant());
        await Assert.That(records[0].Body).IsEqualTo("First log");
        await Assert.That(records[1].TraceId).IsEqualTo(traceId2.ToUpperInvariant());
        await Assert.That(records[1].Body).IsEqualTo("Second log");
    }

    [Test]
    public async Task Parse_LogRecordWithoutTraceId_IsExcluded()
    {
        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec
            {
                SeverityNumber = 9,
                Body = "Startup log with no trace context",
            });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).IsEmpty();
    }

    [Test]
    public async Task Parse_MixOfTracedAndUntracedLogs_OnlyReturnsTraced()
    {
        var traceId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec { SeverityNumber = 9, Body = "No trace" },
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = "Has trace" },
            new LogRecordSpec { SeverityNumber = 17, Body = "Also no trace" });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(1);
        await Assert.That(records[0].Body).IsEqualTo("Has trace");
    }

    [Test]
    public async Task Parse_MultipleResourceLogs_PreservesServiceName()
    {
        var traceId1 = "11111111111111111111111111111111";
        var traceId2 = "22222222222222222222222222222222";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            ("api-gateway", [new LogRecordSpec { TraceId = traceId1, SeverityNumber = 9, Body = "Gateway log" }]),
            ("backend-svc", [new LogRecordSpec { TraceId = traceId2, SeverityNumber = 13, Body = "Backend log" }]));

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(2);
        await Assert.That(records[0].ResourceName).IsEqualTo("api-gateway");
        await Assert.That(records[0].Body).IsEqualTo("Gateway log");
        await Assert.That(records[1].ResourceName).IsEqualTo("backend-svc");
        await Assert.That(records[1].Body).IsEqualTo("Backend log");
    }

    [Test]
    public async Task Parse_NoResource_ResourceNameIsEmpty()
    {
        var traceId = "cccccccccccccccccccccccccccccccc";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            serviceName: null,
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = "Orphaned log" });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(1);
        await Assert.That(records[0].ResourceName).IsEqualTo("");
    }

    [Test]
    public async Task Parse_EmptyBody_BodyIsEmpty()
    {
        var traceId = "dddddddddddddddddddddddddddddddd";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 9 });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(1);
        await Assert.That(records[0].Body).IsEqualTo("");
    }

    [Test]
    public async Task Parse_EmptyPayload_ReturnsEmptyList()
    {
        var records = OtlpLogParser.Parse(ReadOnlySpan<byte>.Empty);

        await Assert.That(records).IsEmpty();
    }

    [Test]
    public async Task Parse_SeverityTextOverridesSeverityNumber()
    {
        var traceId = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec
            {
                TraceId = traceId,
                SeverityNumber = 9,
                SeverityText = "CUSTOM_LEVEL",
                Body = "Custom severity",
            });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records[0].SeverityText).IsEqualTo("CUSTOM_LEVEL");
    }

    [Test]
    public async Task Parse_SpanIdPresent_DoesNotBreakParsing()
    {
        var traceId = "ffffffffffffffffffffffffffffffff";

        var data = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec
            {
                TraceId = traceId,
                SpanId = "1234567890abcdef",
                SeverityNumber = 9,
                Body = "With span ID",
            });

        var records = OtlpLogParser.Parse(data);

        await Assert.That(records).Count().IsEqualTo(1);
        await Assert.That(records[0].Body).IsEqualTo("With span ID");
    }

    // --- FormatSeverity tests ---

    [Test]
    [Arguments(1, "", "TRACE")]
    [Arguments(4, "", "TRACE")]
    [Arguments(5, "", "DEBUG")]
    [Arguments(8, "", "DEBUG")]
    [Arguments(9, "", "INFO")]
    [Arguments(12, "", "INFO")]
    [Arguments(13, "", "WARN")]
    [Arguments(16, "", "WARN")]
    [Arguments(17, "", "ERROR")]
    [Arguments(20, "", "ERROR")]
    [Arguments(21, "", "FATAL")]
    [Arguments(24, "", "FATAL")]
    [Arguments(0, "", "SEV0")]
    [Arguments(25, "", "SEV25")]
    public async Task FormatSeverity_NumberOnly_ReturnsExpected(int number, string text, string expected)
    {
        var result = OtlpLogParser.FormatSeverity(number, text);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task FormatSeverity_TextProvided_PrefersSeverityText()
    {
        var result = OtlpLogParser.FormatSeverity(9, "Warning");

        await Assert.That(result).IsEqualTo("Warning");
    }
}
