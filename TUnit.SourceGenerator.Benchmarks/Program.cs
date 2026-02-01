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
    BenchmarkRunner.Run<TestMetadataGeneratorBenchmarks>();
    // BenchmarkRunner.Run<AotConverterGeneratorBenchmarks>();
    // BenchmarkRunner.Run<StaticPropertyInitializationGeneratorBenchmarks>();
}
