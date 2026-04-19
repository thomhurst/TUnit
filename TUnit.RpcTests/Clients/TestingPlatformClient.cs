using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using TUnit.RpcTests.Models;

namespace TUnit.RpcTests.Clients;

public sealed class TestingPlatformClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly TargetHandler _targetHandler = new();
    private readonly StringBuilder _disconnectionReason = new();

    public TestingPlatformClient(JsonRpc jsonRpc, TcpClient tcpClient)
    {
        JsonRpcClient = jsonRpc;
        _tcpClient = tcpClient;
        JsonRpcClient.AddLocalRpcTarget(
            _targetHandler,
            new JsonRpcTargetOptions
            {
                MethodNameTransform = CommonMethodNameTransforms.CamelCase,
            });

        JsonRpcClient.Disconnected += JsonRpcClient_Disconnected;
        JsonRpcClient.StartListening();
    }

    private void JsonRpcClient_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        _disconnectionReason.AppendLine("Disconnected reason:");
        _disconnectionReason.AppendLine(e.Reason.ToString());
        _disconnectionReason.AppendLine(e.Description);
        _disconnectionReason.AppendLine(e.Exception?.ToString());
    }

    public JsonRpc JsonRpcClient { get; }

    private async Task<T> CheckedInvokeAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception ex) when (_disconnectionReason.Length > 0)
        {
            throw new InvalidOperationException($"{ex.Message}\n{_disconnectionReason}", ex);
        }
    }

    public void RegisterLogListener(LogsCollector listener)
        => _targetHandler.RegisterLogListener(listener);

    public void RegisterTelemetryListener(TelemetryCollector listener)
        => _targetHandler.RegisterTelemetryListener(listener);

    public async Task<InitializeResponse> InitializeAsync(CancellationToken cancellationToken = default)
        => await CheckedInvokeAsync(async () => await JsonRpcClient.InvokeWithParameterObjectAsync<InitializeResponse>(
            "initialize",
            new InitializeRequest(Environment.ProcessId, new ClientInfo("test-client"),
                new ClientCapabilities(new ClientTestingCapabilities(DebuggerProvider: false))),
            cancellationToken: cancellationToken));

    public async Task ExitAsync()
    {
        try
        {
            await JsonRpcClient.NotifyWithParameterObjectAsync("exit", new object());
        }
        catch
        {
            // Best effort — connection may already be gone
        }
    }

    public async Task<ResponseListener> DiscoverTestsAsync(Guid requestId, Func<TestNodeUpdate[], Task> action, CancellationToken cancellationToken = default)
        => await CheckedInvokeAsync(async () =>
        {
            var discoveryListener = new TestNodeUpdatesResponseListener(requestId, action);
            _targetHandler.RegisterResponseListener(discoveryListener);
            await JsonRpcClient.InvokeWithParameterObjectAsync("testing/discoverTests", new DiscoveryRequest(RunId: requestId), cancellationToken: cancellationToken);
            return (ResponseListener)discoveryListener;
        });

    public async Task<ResponseListener> RunTestsAsync(Guid requestId, Func<TestNodeUpdate[], Task> action, TestNode[]? testNodes = null, CancellationToken cancellationToken = default)
        => await CheckedInvokeAsync(async () =>
        {
            var runListener = new TestNodeUpdatesResponseListener(requestId, action);
            _targetHandler.RegisterResponseListener(runListener);
            await JsonRpcClient.InvokeWithParameterObjectAsync("testing/runTests", new RunTestsRequest(RunId: requestId, Tests: testNodes), cancellationToken: cancellationToken);
            return (ResponseListener)runListener;
        });

    public void Dispose()
    {
        JsonRpcClient.Dispose();
        _tcpClient.Dispose();
    }

    public record Log(LogLevel LogLevel, string Message);

    private sealed class TargetHandler
    {
        private readonly ConcurrentDictionary<Guid, ResponseListener> _listeners = new();
        private readonly ConcurrentBag<LogsCollector> _logListeners = [];
        private readonly ConcurrentBag<TelemetryCollector> _telemetryPayloads = [];

        public void RegisterTelemetryListener(TelemetryCollector listener)
            => _telemetryPayloads.Add(listener);

        public void RegisterLogListener(LogsCollector listener)
            => _logListeners.Add(listener);

        public void RegisterResponseListener(ResponseListener responseListener)
            => _ = _listeners.TryAdd(responseListener.RequestId, responseListener);

        [JsonRpcMethod("client/attachDebugger", UseSingleObjectParameterDeserialization = true)]
        public static Task AttachDebuggerAsync(AttachDebuggerInfo attachDebuggerInfo) => throw new NotImplementedException();

        [JsonRpcMethod("testing/testUpdates/tests")]
        public async Task TestsUpdateAsync(Guid runId, TestNodeUpdate[]? changes)
        {
            if (_listeners.TryGetValue(runId, out var responseListener))
            {
                if (changes is null)
                {
                    responseListener.Complete();
                    _listeners.TryRemove(runId, out _);
                    return;
                }

                await responseListener.OnMessageReceiveAsync(changes);
            }
        }

        [JsonRpcMethod("telemetry/update", UseSingleObjectParameterDeserialization = true)]
        public Task TelemetryAsync(TelemetryPayload telemetry)
        {
            foreach (var listener in _telemetryPayloads)
            {
                listener.Add(telemetry);
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod("client/log")]
        public Task LogAsync(LogLevel level, string message)
        {
            foreach (var listener in _logListeners)
            {
                listener.Add(new Log(level, message));
            }

            return Task.CompletedTask;
        }
    }
}

public abstract class ResponseListener(Guid requestId)
{
    private readonly TaskCompletionSource _allMessageReceived = new();

    public Guid RequestId { get; } = requestId;

    public abstract Task OnMessageReceiveAsync(object message);

    internal void Complete() => _allMessageReceived.SetResult();

#pragma warning disable VSTHRD003
    public Task WaitCompletionAsync() => _allMessageReceived.Task;
#pragma warning restore VSTHRD003
}

public sealed class TestNodeUpdatesResponseListener(Guid requestId, Func<TestNodeUpdate[], Task> action)
    : ResponseListener(requestId)
{
    public override async Task OnMessageReceiveAsync(object message)
        => await action((TestNodeUpdate[])message);
}
