using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record TestNodeUpdate
(
    [property: JsonPropertyName("node")]
    TestNode Node,

    [property: JsonPropertyName("parent")]
    string ParentUid);