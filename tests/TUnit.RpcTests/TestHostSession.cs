using System.Net;
using System.Net.Sockets;
using CliWrap;
using StreamJsonRpc;
using TUnit.RpcTests.Clients;
using TUnit.RpcTests.Models;

namespace TUnit.RpcTests;

/// <summary>
/// Owns a running TUnit.TestProject subprocess in <c>--server</c> mode plus the
/// JSON-RPC connection to it. Use <see cref="StartAsync"/> to spin one up and
/// dispose it to tear everything down.
/// </summary>
internal sealed class TestHostSession : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CommandTask<CommandResult> _cliProcess;
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly JsonRpc _rpc;

    public TestingPlatformClient Client { get; }

    private TestHostSession(
        TcpListener listener,
        CommandTask<CommandResult> cliProcess,
        TcpClient tcpClient,
        NetworkStream stream,
        JsonRpc rpc,
        TestingPlatformClient client)
    {
        _listener = listener;
        _cliProcess = cliProcess;
        _tcpClient = tcpClient;
        _stream = stream;
        _rpc = rpc;
        Client = client;
    }

    public static async Task<TestHostSession> StartAsync(string framework, CancellationToken cancellationToken)
    {
        await TestProjectBuilds.EnsureBuiltAsync(framework, cancellationToken);

        var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var cliProcess = Cli.Wrap("dotnet")
            .WithWorkingDirectory(TestProjectBuilds.WorkingDirectory)
            .WithArguments([
                "run",
                "--no-build",
                "-c", "Debug",
                "-f", framework,
                "--server",
                "--client-port", port.ToString()
            ])
            .WithStandardOutputPipe(PipeTarget.Null)
            .WithStandardErrorPipe(PipeTarget.Null)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(cancellationToken);

        var tcpClient = await AcceptWithTimeoutAsync(listener, cliProcess.Task, TimeSpan.FromSeconds(60), port, cancellationToken);

        var stream = tcpClient.GetStream();
        var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter
        {
            JsonSerializerOptions = RpcJsonSerializerOptions.Default
        }));

        var client = new TestingPlatformClient(rpc, tcpClient);

        using (var initCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            initCts.CancelAfter(TimeSpan.FromSeconds(30));
            await client.InitializeAsync(initCts.Token);
        }

        return new TestHostSession(listener, cliProcess, tcpClient, stream, rpc, client);
    }

    private static async Task<TcpClient> AcceptWithTimeoutAsync(
        TcpListener listener,
        Task<CommandResult> cliProcessTask,
        TimeSpan timeout,
        int port,
        CancellationToken cancellationToken)
    {
        var acceptTask = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        var completed = await Task.WhenAny(cliProcessTask, acceptTask, timeoutTask);

        if (completed == timeoutTask)
        {
            throw new TimeoutException($"Timeout waiting for TCP connection after {timeout.TotalSeconds}s on port {port}");
        }

        if (completed == cliProcessTask)
        {
            var result = await cliProcessTask;
            throw new InvalidOperationException($"Test host exited unexpectedly before connecting (exit code {result.ExitCode})");
        }

        return await acceptTask;
    }

    public async Task<List<TestNodeUpdate>> DiscoverAsync(CancellationToken cancellationToken)
    {
        var updates = new List<TestNodeUpdate>();
        var response = await Client.DiscoverTestsAsync(Guid.NewGuid(), batch =>
        {
            lock (updates) updates.AddRange(batch);
            return Task.CompletedTask;
        }, cancellationToken);

        await response.WaitCompletionAsync();
        return updates;
    }

    public async Task<List<TestNodeUpdate>> RunAsync(TestNode[]? tests, CancellationToken cancellationToken)
    {
        var updates = new List<TestNodeUpdate>();
        var response = await Client.RunTestsAsync(Guid.NewGuid(), batch =>
        {
            lock (updates) updates.AddRange(batch);
            return Task.CompletedTask;
        }, tests, cancellationToken);

        await response.WaitCompletionAsync();
        return updates;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Client.ExitAsync();
        }
        finally
        {
            Client.Dispose();
            await _stream.DisposeAsync();
            _tcpClient.Dispose();
            _rpc.Dispose();
            _listener.Stop();

            try
            {
                using var killCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _cliProcess.Task.WaitAsync(killCts.Token);
            }
            catch
            {
                // Best effort — subprocess may already be gone or stuck
            }
        }
    }
}
