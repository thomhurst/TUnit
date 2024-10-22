using System.Net;
using System.Net.Sockets;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Testing.Platform.Extensions.Messages;
using StreamJsonRpc;
using TUnit.RpcTests.RpcModels;

namespace TUnit.RpcTests;

public class Tests
{
    [CancelAfter(300_000)]
    [Test]
    public async Task Test1(CancellationToken cancellationToken)
    {
        await RunTests(cancellationToken);
    }

    private async Task RunTests(CancellationToken cancellationToken)
    {
        using var signalEvent = new ManualResetEventSlim(false);

        // Open a port that the test could listen on
        var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
        listener.Start();

        await using var _ = cancellationToken.Register(() => listener.Stop());
        
        // Start the test host and accept the connection from the test host
        var cliProcess = Cli.Wrap("dotnet")
            .WithWorkingDirectory(@"C:\git\TUnit\TUnit.TestProject")
            .WithArguments([
                "run",
                "-f", "net8.0",
                "--server", 
                "--client-port",
                ((IPEndPoint)listener.LocalEndpoint).Port.ToString()
            ])
            .ExecuteBufferedAsync(cancellationToken: cancellationToken);

        var sendRequestsTask = SendRequests(listener, cancellationToken);

        await cliProcess;

        var testNodeUpdates = await sendRequestsTask;
        
        var discovered = testNodeUpdates.Where(x => GetState(x.TestNode) is DiscoveredTestNodeStateProperty).ToList();
        var passed = testNodeUpdates.Where(x => GetState(x.TestNode) is PassedTestNodeStateProperty).ToList();
        var failed = testNodeUpdates.Where(x => GetState(x.TestNode) is FailedTestNodeStateProperty).ToList();
        var skipped = testNodeUpdates.Where(x => GetState(x.TestNode) is SkippedTestNodeStateProperty).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(discovered, Has.Count.EqualTo(10));
            Assert.That(passed, Has.Count.EqualTo(10));
            Assert.That(failed, Has.Count.EqualTo(10));
            Assert.That(skipped, Has.Count.EqualTo(10));
        });
    }

    private static async Task<List<TestNodeUpdateMessage>> SendRequests(TcpListener listener, CancellationToken cancellationToken)
    {
        using var tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);

        var stream = tcpClient.GetStream();
        var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter
        {
            JsonSerializerOptions = RpcJsonSerializerOptions.Default
        }));
        
        var callback = new CallbackHandler();
        rpc.AddLocalRpcTarget(callback);
        rpc.StartListening();

        // Send the initialize request alongside the discovery request:
        // https://github.com/microsoft/testfx/blob/main/src/Platform/Microsoft.Testing.Platform/ServerMode/JsonRpc/RpcMessages.cs
        // contains the shapes of payloads
        var discoveryId = Guid.NewGuid();
        await rpc.InvokeWithParameterObjectAsync<InitializeResponseArgs>("initialize", new InitializeRequestArgs(Environment.ProcessId, new ClientInfo("RpcTests", "1.0.0"), new ClientCapabilities(new ClientTestingCapabilities(false, false))), cancellationToken);
        await rpc.InvokeWithParameterObjectAsync("testing/discoverTests", new DiscoverRequestArgs(discoveryId, null, null), cancellationToken);

        // Wait for callback handler to send back the empty message.
        // Verify the messages received.
        
        // Request the server to shutdown and wait for the process to exit.
        await rpc.NotifyWithParameterObjectAsync("exit", new object());
        
        await rpc.Completion;

        return callback.TestNodeUpdates;
    }

    private static TestNodeStateProperty GetState(TestNode node) =>
        node.Properties.OfType<TestNodeStateProperty>().First()!;

    public class CallbackHandler
    {
        public readonly List<TestNodeUpdateMessage> TestNodeUpdates = [];
        
        [JsonRpcMethod("testing/testUpdates/tests", UseSingleObjectParameterDeserialization = true)]
        public Task TestsUpdateAsync(TestNodeStateChangedEventArgs testNodeStateChangedEventArgs)
        {
            TestNodeUpdates.AddRange(testNodeStateChangedEventArgs.Changes ?? []);
            return Task.CompletedTask;
        }

        [JsonRpcMethod("exit")]
        public void Exit(object? obj)
        {
            Console.WriteLine("Exit notification received. Shutting down...");
        }
    }
}