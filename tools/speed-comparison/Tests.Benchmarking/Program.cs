using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using Tests.Benchmarking;

var summary = BenchmarkRunner.Run<Benchmarks>(ManualConfig.CreateMinimumViable().AddExporter(MarkdownExporter.GitHub));