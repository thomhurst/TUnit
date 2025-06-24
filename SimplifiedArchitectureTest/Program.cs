using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using SimplifiedArchitectureTest;
using TUnit.Engine.Extensions;

// Check for debug mode
if (args.Contains("--debug-runner"))
{
    await DebugRunner.RunDebug();
    return 0;
}

if (args.Contains("--trace"))
{
    return await SimpleTrace.RunWithTrace(args.Where(a => a != "--trace").ToArray());
}

Console.WriteLine("Starting test with exit behavior check...");

// Add timeout to force exit
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    var builder = await TestApplication.CreateBuilderAsync(args);
    builder.AddSimplifiedTUnit();
    
    using var app = await builder.BuildAsync();
    
    var exitCode = await app.RunAsync();
    
    Console.WriteLine($"Test run completed with exit code: {exitCode}");
    return exitCode;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Test run timed out after 10 seconds");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"Test run failed: {ex}");
    return 2;
}