using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record TestNode
{
    [JsonPropertyName("uid")]
    public required string Uid { get; init; }

    [JsonPropertyName("display-name")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("node-type")]
    public string? NodeType { get; init; }

    [JsonPropertyName("execution-state")]
    public string? ExecutionState { get; init; }

    // Captures every other server-sent field (traits, location.*, error.*, etc.)
    // so that when we round-trip the node back in testCases, the server sees
    // the full payload it originally produced.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
