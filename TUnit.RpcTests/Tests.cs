using System.Net;
using System.Net.Sockets;
using CliWrap;
using StreamJsonRpc;
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
            .WithWorkingDirectory(Sourcy.DotNet.Projects.TUnit_TestProject.DirectoryName!)
            .WithArguments([
                "run",
                "-f", "net8.0",
                "--server",
                "--client-port",
                ((IPEndPoint)listener.LocalEndpoint).Port.ToString()
            ])
            .WithStandardOutputPipe(PipeTarget.Merge(outputPipe, PipeTarget.ToDelegate(Console.WriteLine)))
            .WithStandardErrorPipe(PipeTarget.Merge(outputPipe, PipeTarget.ToDelegate(Console.WriteLine)))
            .ExecuteAsync(cancellationToken: cancellationToken);

        var tcpClientTask = listener.AcceptTcpClientAsync(cancellationToken).AsTask();

        // Will throw if either the server fails or the TCP call fails
        await await Task.WhenAny(cliProcess, tcpClientTask);

        using var tcpClient = await tcpClientTask;

        await using var stream = tcpClient.GetStream();

        using var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter
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
            await Assert.That(originalDiscovered).HasCount().GreaterThanOrEqualTo(3400);

            await client.ExitAsync();
        }
    }
}
