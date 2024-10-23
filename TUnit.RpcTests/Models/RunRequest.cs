using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record RunRequest(
    [property:JsonPropertyName("runId")]
    Guid RunId);
