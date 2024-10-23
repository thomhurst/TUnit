using System.Text.Json.Serialization;

namespace TUnit.RpcTests.Models;

public sealed record AttachDebuggerInfo(
    [property:JsonPropertyName("processId")]
    int ProcessId);
