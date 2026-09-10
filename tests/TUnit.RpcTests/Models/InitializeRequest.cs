using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record InitializeRequest(
    [property:JsonPropertyName("processId")]
    int ProcessId,

    [property:JsonPropertyName("clientInfo")]
    ClientInfo ClientInfo,

    [property:JsonPropertyName("capabilities")]
    ClientCapabilities Capabilities);
