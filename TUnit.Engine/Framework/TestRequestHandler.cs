using System.Reflection;
using System.Text;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest:
                await HandleDiscoveryRequestAsync(serviceProvider, context, testExecutionFilter);
                break;

            case RunTestExecutionRequest runRequest:
                await HandleRunRequestAsync(serviceProvider, runRequest, context, testExecutionFilter);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.GetType().Name,
                    "Unknown request type");
        }
    }

    private async Task HandleDiscoveryRequestAsync(
        TUnitServiceProvider serviceProvider,
        ExecuteRequestContext context,
        ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: false);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        foreach (var test in discoveryResult.Tests)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            await serviceProvider.MessageBus.Discovered(test.Context);
        }
    }

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter)
    {
        var discoveryResult = await serviceProvider.DiscoveryService.DiscoverTests(context.Request.Session.SessionUid.Value, testExecutionFilter, context.CancellationToken, isForExecution: true);

#if NET
        if (discoveryResult.ExecutionContext != null)
        {
            ExecutionContext.Restore(discoveryResult.ExecutionContext);
        }
#endif

        var allTests = discoveryResult.Tests.ToArray();

        // Skip sending Discovered messages during execution - they're only needed for discovery requests
        // This saves significant time and allocations when running tests

        await serviceProvider.TestSessionCoordinator.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);

        // Export dependency graph if requested via command line
        await ExportDependencyGraphIfRequestedAsync(serviceProvider.CommandLineOptions, allTests);
    }

    private static async Task ExportDependencyGraphIfRequestedAsync(
        ICommandLineOptions commandLineOptions,
        AbstractExecutableTest[] allTests)
    {
        if (!commandLineOptions.TryGetOptionArgumentList(
                DependencyGraphCommandProvider.ExportDependencyGraph, out var args))
        {
            return;
        }

        var testList = new List<AbstractExecutableTest>(allTests);
        var mermaidContent = DependencyGraphExporter.GenerateMermaidDiagram(testList);

        if (string.IsNullOrEmpty(mermaidContent))
        {
            Console.WriteLine("Dependency graph: no dependencies found among tests. Skipping .mmd file generation.");
            return;
        }

        var outputPath = ResolveOutputPath(args.Length > 0 ? args[0] : null);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

#if NET
        await File.WriteAllTextAsync(outputPath, mermaidContent, Encoding.UTF8);
#else
        File.WriteAllText(outputPath, mermaidContent, Encoding.UTF8);
        await Task.CompletedTask;
#endif

        Console.WriteLine($"Dependency graph written to: {outputPath}");
    }

    private static string ResolveOutputPath(string? userProvidedPath)
    {
        if (string.IsNullOrWhiteSpace(userProvidedPath))
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
            return Path.Combine("TestResults", $"{assemblyName}-dependencies.mmd");
        }

        var path = userProvidedPath!;

        if (!path.EndsWith(".mmd", StringComparison.OrdinalIgnoreCase))
        {
            path += ".mmd";
        }

        return path;
    }
}
