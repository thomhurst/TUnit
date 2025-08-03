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
    [Test, Skip("TODO: Fix RPC tests")]
    public async Task TestAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if we can bind to localhost
            await CheckNetworkConnectivity();

            // Run all tests (no filter)
            await RunTestsAsync(cancellationToken, testUidFilter: node => node.Uid.StartsWith("TUnit.TestProject.BasicTests.1.1"));

            // Or run specific tests by filtering on discovered test UIDs
            // Example: await RunTestsAsync(cancellationToken, testUidFilter: uid => uid.Contains("BasicTests"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Test] Test failed with exception: {ex}");
            throw;
        }
    }

    private async Task CheckNetworkConnectivity()
    {
        Console.WriteLine("[RPC Test] Checking network connectivity...");

        try
        {
            // Test if we can create a TCP listener
            using var testListener = new TcpListener(IPAddress.Loopback, 0);
            testListener.Start();
            var testPort = ((IPEndPoint)testListener.LocalEndpoint).Port;
            Console.WriteLine($"[RPC Test] Successfully created test listener on port {testPort}");

            // Test if we can connect to ourselves
            using var testClient = new TcpClient();
            await testClient.ConnectAsync(IPAddress.Loopback, testPort);
            Console.WriteLine("[RPC Test] Successfully connected to test listener");

            testListener.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPC Test] Network connectivity check failed: {ex.Message}");
            throw new InvalidOperationException("Network connectivity check failed. This might be due to firewall restrictions.", ex);
        }
    }

    private async Task RunTestsAsync(CancellationToken cancellationToken, Func<TestNode, bool>? testUidFilter = null)
    {
        Console.WriteLine($"[RPC Test] Starting RPC test at {DateTime.Now:HH:mm:ss.fff}");

        // Open a port that the test could listen on
        var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Console.WriteLine($"[RPC Test] TCP listener started on port {port}");

        await using var _ = cancellationToken.Register(() =>
        {
            Console.WriteLine("[RPC Test] Cancellation requested, stopping listener");
            listener.Stop();
        });

        await using var output = new MemoryStream();

        var outputPipe = PipeTarget.ToStream(output);

        // Start the test host and accept the connection from the test host
        var workingDir = Sourcy.DotNet.Projects.TUnit_TestProject.DirectoryName!;
        Console.WriteLine($"[RPC Test] Starting test host process in: {workingDir}");

        // Verify working directory exists
        if (!Directory.Exists(workingDir))
        {
            throw new DirectoryNotFoundException($"Test project directory not found: {workingDir}");
        }

        // Check if project file exists
        var projectFile = Path.Combine(workingDir, "TUnit.TestProject.csproj");
        if (!File.Exists(projectFile))
        {
            throw new FileNotFoundException($"Project file not found: {projectFile}");
        }

        Console.WriteLine($"[RPC Test] Command: dotnet run -f net8.0 --server --client-port {port}");

        var cliProcess = Cli.Wrap("dotnet")
            .WithWorkingDirectory(workingDir)
            .WithArguments([
                "run",
                "-f", "net8.0",
                "--server",
                "--client-port",
                port.ToString()
            ])
            .WithStandardOutputPipe(PipeTarget.Merge(
                outputPipe,
                PipeTarget.ToDelegate(line => Console.WriteLine($"[TestHost] {line}"))
            ))
            .WithStandardErrorPipe(PipeTarget.Merge(
                outputPipe,
                PipeTarget.ToDelegate(line => Console.WriteLine($"[TestHost ERR] {line}"))
            ))
            .ExecuteAsync(cancellationToken: cancellationToken);

        Console.WriteLine("[RPC Test] Waiting for TCP connection from test host...");
        var tcpClientTask = listener.AcceptTcpClientAsync(cancellationToken).AsTask();

        // Add timeout for connection
        var connectionTimeout = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        var cliProcessTask = cliProcess.Task;
        var completedTask = await Task.WhenAny(cliProcessTask, tcpClientTask, connectionTimeout);

        if (completedTask == connectionTimeout)
        {
            throw new TimeoutException($"[RPC Test] Timeout waiting for TCP connection after 30 seconds. Port: {port}");
        }

        if (completedTask == cliProcessTask)
        {
            var result = await cliProcessTask;
            throw new InvalidOperationException($"[RPC Test] Test host process exited unexpectedly with code {result.ExitCode}");
        }

        Console.WriteLine("[RPC Test] TCP connection established!");
        using var tcpClient = await tcpClientTask;

        await using var stream = tcpClient.GetStream();

        Console.WriteLine("[RPC Test] Creating JSON-RPC connection...");
        using var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, new SystemTextJsonFormatter
        {
            JsonSerializerOptions = RpcJsonSerializerOptions.Default
        }));

        Console.WriteLine("[RPC Test] Creating TestingPlatformClient...");
        using var client = new TestingPlatformClient(rpc, tcpClient, new ProcessHandle(cliProcess, output), enableDiagnostic: true);

        Console.WriteLine("[RPC Test] Initializing client...");
        var initTask = client.InitializeAsync();
        var initTimeout = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        var cliProcessTask2 = cliProcess.Task;

        var initCompleted = await Task.WhenAny(cliProcessTask2, initTask, initTimeout);
        if (initCompleted == initTimeout)
        {
            throw new TimeoutException("[RPC Test] Timeout during client initialization after 10 seconds");
        }
        if (initCompleted == cliProcessTask2)
        {
            var result = await cliProcessTask2;
            throw new InvalidOperationException($"[RPC Test] Test host exited during initialization with code {result.ExitCode}");
        }

        var initResponse = await initTask;
        Console.WriteLine($"[RPC Test] Client initialized successfully. Server info: {initResponse.ServerInfo.Name}");

        var discoveryId = Guid.NewGuid();
        Console.WriteLine($"[RPC Test] Starting test discovery with ID: {discoveryId}");

        List<TestNodeUpdate> discoveredResults = [];
        var updateCount = 0;
        var discoverTestsResponse = await client.DiscoverTestsAsync(discoveryId, updates =>
        {
            updateCount++;
            discoveredResults.AddRange(updates);
            Console.WriteLine($"[RPC Test] Received discovery update #{updateCount}: {updates.Length} tests");
            return Task.CompletedTask;
        });

        await Assert.That(discoveredResults).HasCount().GreaterThan(4000);

        Console.WriteLine("[RPC Test] Waiting for discovery to complete...");
        await discoverTestsResponse.WaitCompletionAsync();
        Console.WriteLine($"[RPC Test] Discovery completed. Total updates: {updateCount}, Total tests: {discoveredResults.Count}");

        var originalDiscovered = discoveredResults.Where(x => x.Node.ExecutionState is "discovered").ToList();

        // Apply filter if provided
        TestNode[]? testNodes = null;
        if (testUidFilter != null)
        {
            testNodes = originalDiscovered
                .Select(x => x.Node)
                .Where(testUidFilter)
                .ToArray();

            Console.WriteLine($"[RPC Test] Filtered to {testNodes.Length} tests out of {originalDiscovered.Count}");
        }

        discoveredResults.Clear();
        updateCount = 0;
        Console.WriteLine($"[RPC Test] Starting test execution with {testNodes?.Length ?? originalDiscovered.Count} tests");
        var executeTestsResponse = await client.RunTestsAsync(discoveryId, updates =>
        {
            updateCount++;
            discoveredResults.AddRange(updates);
            var inProgress = updates.Count(x => x.Node.ExecutionState == "in-progress");
            var finished = updates.Count(x => x.Node.ExecutionState != "in-progress");
            Console.WriteLine($"[RPC Test] Execution update #{updateCount}: {inProgress} in progress, {finished} finished");
            return Task.CompletedTask;
        }, testNodes);

        Console.WriteLine("[RPC Test] Waiting for execution to complete...");
        await executeTestsResponse.WaitCompletionAsync();
        Console.WriteLine($"[RPC Test] Execution completed. Total updates: {updateCount}");

        var finished = discoveredResults.Where(x => x.Node.ExecutionState is not "in-progress").ToList();

        using (Assert.Multiple())
        {
            if (testUidFilter != null)
            {
                // With filter, we expect fewer tests
                Console.WriteLine($"[RPC Test] With filter, executed test count: {finished.Count}");
                await Assert.That(finished).HasCount().GreaterThan(0);
                await Assert.That(finished).HasCount().LessThan(originalDiscovered.Count);
            }
            else
            {
                // Without filter, expect all tests
                Console.WriteLine($"[RPC Test] Asserting discovered test count: {originalDiscovered.Count} (expected >= 3400)");
                await Assert.That(originalDiscovered).HasCount().GreaterThanOrEqualTo(3400);
            }

            Console.WriteLine("[RPC Test] Sending exit command to test host...");
            await client.ExitAsync();
            Console.WriteLine("[RPC Test] Test completed successfully!");
        }
    }
}
