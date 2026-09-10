using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record DiscoveryRequest(
    [property:JsonPropertyName("runId")]
    Guid RunId);
