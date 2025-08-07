using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using TUnit.Performance.Tests;

// Configure for CI if environment variable is set
var config = DefaultConfig.Instance;

if (Environment.GetEnvironmentVariable("CI") == "true" || args.Contains("--ci"))
{
    config = config
        .WithOptions(ConfigOptions.DisableOptimizationsValidator)
        .AddExporter(JsonExporter.Brief)
        .AddExporter(MarkdownExporter.GitHub)
        .AddLogger(ConsoleLogger.Default);
    
    Console.WriteLine("Running in CI mode - generating JSON and Markdown reports");
}

// Run specific benchmark if specified, otherwise run all
if (args.Length > 0 && !args.Contains("--ci"))
{
    var benchmarkType = args[0] switch
    {
        "discovery" => typeof(TestDiscoveryBenchmarks),
        "execution" => typeof(TestExecutionBenchmarks),
        "datasource" => typeof(DataSourceBenchmarks),
        "issue2756" => typeof(Issue2756RegressionBenchmark),
        "optimizations" => typeof(OptimizationBenchmarks),
        _ => null
    };
    
    if (benchmarkType != null)
    {
        BenchmarkRunner.Run(benchmarkType, config);
    }
    else
    {
        Console.WriteLine($"Unknown benchmark: {args[0]}");
        Console.WriteLine("Available benchmarks: discovery, execution, datasource, issue2756, optimizations");
    }
}
else
{
    // Run all benchmarks
    var summary1 = BenchmarkRunner.Run<TestDiscoveryBenchmarks>(config);
    var summary2 = BenchmarkRunner.Run<TestExecutionBenchmarks>(config);
    var summary3 = BenchmarkRunner.Run<DataSourceBenchmarks>(config);
    var summary4 = BenchmarkRunner.Run<Issue2756RegressionBenchmark>(config);
    var summary5 = BenchmarkRunner.Run<OptimizationBenchmarks>(config);
    
    // Check for regressions if in CI
    if (Environment.GetEnvironmentVariable("CI") == "true" || args.Contains("--ci"))
    {
        CheckForRegressions(summary4);
    }
}

void CheckForRegressions(BenchmarkDotNet.Reports.Summary? summary)
{
    if (summary == null) return;
    
    // Check if Issue #2756 benchmark meets performance targets
    var issue2756Result = summary.Reports
        .FirstOrDefault(r => r.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo.Contains("Issue2756"));
    
    if (issue2756Result != null)
    {
        var meanTime = issue2756Result.ResultStatistics?.Mean ?? 0;
        var meanTimeMs = meanTime / 1_000_000; // Convert nanoseconds to milliseconds
        
        if (meanTimeMs > 200)
        {
            Console.WriteLine($"⚠️ PERFORMANCE REGRESSION DETECTED!");
            Console.WriteLine($"Issue #2756 benchmark took {meanTimeMs:F2}ms (target: <200ms)");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine($"✅ Performance target met: {meanTimeMs:F2}ms (target: <200ms)");
        }
    }
}