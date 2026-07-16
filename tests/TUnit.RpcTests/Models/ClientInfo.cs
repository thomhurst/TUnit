using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record ClientInfo(
    [property:JsonPropertyName("name")]
    string Name,

    [property:JsonPropertyName("version")]
    string Version = "1.0.0");
