using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record ClientTestingCapabilities(
    [property: JsonPropertyName("debuggerProvider")]
    bool DebuggerProvider);