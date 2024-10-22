using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace TUnit.RpcTests.RpcModels;

public abstract record RpcMessage;

/// <summary>
/// A request is a message for which the server should return a corresponding
/// <see cref="ErrorMessage"/> or <see cref="ResponseMessage"/>.
/// </summary>
public record RequestMessage(int Id, string Method, object? Params) : RpcMessage;

/// <summary>
/// A notification message is a message that notifies the server of an event.
/// There's no corresponding response that the server should send back and as such
/// no id is specified when sending a notification.
/// </summary>
public record NotificationMessage(string Method, object? Params) : RpcMessage;

/// <summary>
/// An error message is sent if some exception was thrown when processing the request.
/// </summary>
public record ErrorMessage(int Id, int ErrorCode, string Message, object? Data) : RpcMessage;

/// <summary>
/// An response message is sent if a request is handled successfully.
/// </summary>
/// <remarks>
/// If the RPC handler returns a <see cref="Task"/> the <paramref name="Result"/>
/// will be returned as <c>null</c>.
/// </remarks>
public record ResponseMessage(int Id, object? Result) : RpcMessage;

public record InitializeRequestArgs(int ProcessId, ClientInfo ClientInfo, ClientCapabilities Capabilities);

public record InitializeResponseArgs(int? ProcessId, ServerInfo ServerInfo, ServerCapabilities Capabilities);

public record RequestArgsBase(Guid RunId, ICollection<TestNode>? TestNodes, string? GraphFilter);

public record DiscoverRequestArgs(Guid RunId, ICollection<TestNode>? TestNodes, string? GraphFilter) :
    RequestArgsBase(RunId, TestNodes, GraphFilter);

public record ResponseArgsBase;

public record DiscoverResponseArgs : ResponseArgsBase;

public record RunRequestArgs(Guid RunId, ICollection<TestNode>? TestNodes, string? GraphFilter) :
    RequestArgsBase(RunId, TestNodes, GraphFilter);

public record RunResponseArgs(Artifact[] Artifacts) : ResponseArgsBase;

public record Artifact(string Uri, string Producer, string Type, string DisplayName, string? Description = null);

public record CancelRequestArgs(int CancelRequestId);

public record ExitRequestArgs;

public record ClientInfo(string Name, string Version);

public record ClientCapabilities(ClientTestingCapabilities Testing);

public record ClientTestingCapabilities(bool DebuggerProvider, bool AttachmentsProvider);

public record ServerInfo(string Name, string Version);

public record ServerCapabilities(ServerTestingCapabilities TestingCapabilities);

public record ServerTestingCapabilities(
    bool SupportsDiscovery,
    bool MultiRequestSupport,
    bool VsTestProviderSupport,
    bool SupportsAttachments,
    bool MultiConnectionProvider);

public record TestNodeStateChangedEventArgs(Guid RunId, TestNodeUpdateMessage[]? Changes);

public record TelemetryEventArgs(string EventName, IDictionary<string, object> Metrics);

public record ProcessInfoArgs(string Program, string? Args, string? WorkingDirectory, IDictionary<string, string?>? EnvironmentVariables);

public record AttachDebuggerInfoArgs(int ProcessId);

public record TestsAttachments(RunTestAttachment[] Attachments);

public record RunTestAttachment(string? Uri, string? Producer, string? Type, string? DisplayName, string? Description);