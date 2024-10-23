using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record ClientCapabilities(
    [property: JsonPropertyName("testing")]
    ClientTestingCapabilities Testing);