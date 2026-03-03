using System.Text.Json.Serialization;

namespace TUnit.Engine.Reporters.Html;

[JsonSerializable(typeof(ReportData))]
[JsonSerializable(typeof(ReportSummary))]
[JsonSerializable(typeof(ReportTestGroup))]
[JsonSerializable(typeof(ReportTestResult))]
[JsonSerializable(typeof(ReportExceptionData))]
[JsonSerializable(typeof(ReportKeyValue))]
[JsonSerializable(typeof(SpanData))]
[JsonSerializable(typeof(SpanEvent))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal sealed partial class HtmlReportJsonContext : JsonSerializerContext;
