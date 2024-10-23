using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record ServerCapabilities(
    [property: JsonPropertyName("testing")]
    ServerTestingCapabilities Testing);