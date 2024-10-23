using System.Text.Json.Serialization;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.RpcTests.Models;

public record TestNodeStateChangedEventArgs(
    [property:JsonPropertyName("runId")] Guid RunId, 
    [property:JsonPropertyName("changes")] TestNodeUpdate[] Changes
    );