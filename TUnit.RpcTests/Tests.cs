using System.Net;
using System.Net.Sockets;
using CliWrap;
using StreamJsonRpc;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.RpcTests.Clients;
using TUnit.RpcTests.Models;

namespace TUnit.RpcTests;

public class Tests
{
    [Timeout(300_000)]
    [Retry(3)]
    [Test]
    public async Task TestAsync(CancellationToken cancellationToken)
    {
        await RunTestsAsync(cancellationToken);
    }

    private async Task RunTestsAsync(CancellationToken cancellationToken)
    {
        // Open a port that the test could listen on
        var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
        listener.Start();
        
        await using var _ = cancellationToken.Register(() => listener.Stop());

        await using var output = new MemoryStream();
        
        var outputPipe = PipeTarget.ToStream(output);
        
        // Start the test host and accept the connection from the test host
        var cliProcess = Cli.Wrap("dotnet")
            .WithWorkingDirectory(@"C:\git\TUnit\TUnit.TestProject")
            .WithArguments([
                "run",
                "-f", "net8.0",
                // "-c", "Debug",
                // "-p:LaunchDebugger=true",
                "--server", 
                "--client-port",
                ((IPEndPoint)listener.LocalEndpoint).Port.ToString()
            ])
            .WithStandardOutputPipe(outputPipe)
            .WithStandardErrorPipe(outputPipe)
            .ExecuteAsync(cancellationToken: cancellationToken);

        var tcpClientTask = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
        
        // Will throw if either the server fails or the TCP call fails
        await await Task.WhenAny(cliProcess.Task, tcpClientTask);
        
        using var tcpClient = await tcpClientTask;

        var stream = tcpClient.GetStream();
        var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter
        {
            JsonSerializerOptions = RpcJsonSerializerOptions.Default
        }));
        
        using var client = new TestingPlatformClient(rpc, tcpClient, new ProcessHandle(cliProcess, output));
        
        await await Task.WhenAny(cliProcess, client.InitializeAsync());
        
        var discoveryId = Guid.NewGuid();

        List<TestNodeUpdate> results = [];
        var discoverTestsResponse = await client.DiscoverTestsAsync(discoveryId, updates =>
        {
            results.AddRange(updates);
            return Task.CompletedTask;
        });

        await discoverTestsResponse.WaitCompletionAsync();

        var originalDiscovered = results.Where(x => x.Node.ExecutionState is "discovered").ToList();
        
        results.Clear();
        var executeTestsResponse = await client.RunTestsAsync(discoveryId, updates =>
        {
            results.AddRange(updates);
            return Task.CompletedTask;
        });

        await executeTestsResponse.WaitCompletionAsync();

        var newDiscovered = results.Where(x => x.Node.ExecutionState is "discovered").ToList();
        var finished = results.Where(x => x.Node.ExecutionState is not "in-progress").ToList();
        var passed = finished.Where(x => x.Node.ExecutionState == "passed").ToList();
        var failed = finished.Where(x => x.Node.ExecutionState == "failed").ToList();
        var skipped = finished.Where(x => x.Node.ExecutionState == "skipped").ToList();

        using (Assert.Multiple())
        {
            await Assert.That(originalDiscovered).HasCount().GreaterThanOrEqualTo(1185);
            await Assert.That(newDiscovered).HasCount().EqualToZero();
            await Assert.That(finished).HasCount().GreaterThanOrEqualTo(1186);
            await Assert.That(passed).HasCount().GreaterThanOrEqualTo(929);
            await Assert.That(failed).HasCount().GreaterThanOrEqualTo(88);
            await Assert.That(skipped).HasCount().GreaterThanOrEqualTo(7);
        }

        await client.ExitAsync();
    }
}