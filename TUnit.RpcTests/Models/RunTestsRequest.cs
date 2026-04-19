using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record RunTestsRequest(
    [property:JsonPropertyName("runId")]
    Guid RunId,
    [property:JsonPropertyName("tests")]
    TestNode[]? Tests = null);
