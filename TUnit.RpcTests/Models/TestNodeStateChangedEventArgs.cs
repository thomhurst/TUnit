using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public record TestNodeStateChangedEventArgs(
    [property:JsonPropertyName("runId")] Guid RunId, 
    [property:JsonPropertyName("changes")] TestNodeUpdate[] Changes
    );