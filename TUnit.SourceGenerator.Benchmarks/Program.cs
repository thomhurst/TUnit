using BenchmarkDotNet.Running;
using Microsoft.Build.Locator;
using TUnit.SourceGenerator.Benchmarks;

MSBuildLocator.RegisterDefaults();

if (args is { Length: > 0 })
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
else
{
    var bench  = new TestMetadataGeneratorBenchmarks();
    bench.SetupRunGenerator();
    for (int i = 0; i < 100; i++)
    {
        bench.RunGenerator();
        var count = TUnit.Core.SourceGenerator.Helpers.InterfaceCache._implementsCache.Count;
        Console.WriteLine($"{count} references to stale compilations. Total {GC.GetTotalMemory(true)/1_000_000} MB");
    }

    bench.Cleanup();
    // BenchmarkRunner.Run<TestMetadataGeneratorBenchmarks>();
    // BenchmarkRunner.Run<AotConverterGeneratorBenchmarks>();
    // BenchmarkRunner.Run<StaticPropertyInitializationGeneratorBenchmarks>();
}
