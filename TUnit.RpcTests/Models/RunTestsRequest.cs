using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record RunTestsRequest(
    [property:JsonPropertyName("runId")]
    Guid RunId,
    [property:JsonPropertyName("testCases")]
    TestNode[]? TestCases = null);
