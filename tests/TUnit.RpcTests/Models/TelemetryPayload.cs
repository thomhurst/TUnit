using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public record TelemetryPayload
(
    [property: JsonPropertyName(nameof(TelemetryPayload.EventName))]
    string EventName,

    [property: JsonPropertyName("metrics")]
    IDictionary<string, string> Metrics);
