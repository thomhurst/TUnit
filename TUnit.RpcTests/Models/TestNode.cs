using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

// TODO: complete the object model
public sealed record TestNode
(
    [property: JsonPropertyName("uid")]
    string Uid,

    [property: JsonPropertyName("display-name")]
    string DisplayName,

    [property: JsonPropertyName("node-type")]
    string NodeType,

    [property: JsonPropertyName("execution-state")]
    string ExecutionState);
