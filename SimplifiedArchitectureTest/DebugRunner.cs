using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using TUnit.Engine;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace SimplifiedArchitectureTest;

public static class DebugRunner
{
    public static async Task RunDebug()
    {
        Console.WriteLine("Starting debug runner...");

        var sources = TestMetadataRegistry.GetSources();
        Console.WriteLine($"Found {sources.Count()} test metadata sources");

        foreach (var source in sources)
        {
            var metadata = await source.GetTestMetadata();
            Console.WriteLine($"Source returned {metadata.Count()} test metadata items");
        }

        // Create simplified message bus that logs
        var messageBus = new DebugMessageBus();

        // Create test discovery service
        var discoveryService = new TestDiscoveryService(
            TestMetadataRegistry.GetSources(),
            new TestFactory(
                new DefaultTestInvoker(),
                new DefaultHookInvoker(),
                new DefaultDataSourceResolver()),
            enableDynamicDiscovery: false);

        Console.WriteLine("Discovering tests...");
        var tests = (await discoveryService.DiscoverTests()).ToList();
        Console.WriteLine($"Discovered {tests.Count} executable tests");

        foreach (var test in tests)
        {
            Console.WriteLine($"  - {test.DisplayName} (ID: {test.TestId})");
        }

        // Try to execute one test directly
        if (tests.Any())
        {
            Console.WriteLine("\nExecuting first test directly...");
            var firstTest = tests.First();
            
            var executor = new DefaultSingleTestExecutor(new MockLogger());
            var result = await executor.ExecuteTestAsync(firstTest, messageBus, CancellationToken.None);
            
            Console.WriteLine($"Test result: {result.TestNode.DisplayName}");
            Console.WriteLine("Properties:");
            foreach (var prop in result.TestNode.Properties.AsEnumerable())
            {
                Console.WriteLine($"  - {prop.GetType().Name}");
            }
        }

        Console.WriteLine("\nDebug runner completed!");
    }
}

// Simple message bus for debugging
class DebugMessageBus : IMessageBus
{
    public Task PublishAsync(IDataProducer dataProducer, IData data)
    {
        if (data is TestNodeUpdateMessage update)
        {
            Console.WriteLine($"[MessageBus] Test update: {update.TestNode.DisplayName}");
            foreach (var prop in update.TestNode.Properties.AsEnumerable())
            {
                Console.WriteLine($"[MessageBus]   Property: {prop.GetType().Name}");
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose() { }
}