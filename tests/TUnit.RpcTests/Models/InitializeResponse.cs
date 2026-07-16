namespace TUnit.RpcTests.Models;

public sealed record InitializeResponse(
    ServerInfo ServerInfo,
    ServerCapabilities Capabilities);
