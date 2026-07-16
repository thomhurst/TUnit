using BenchmarkDotNet.Running;
using TUnit.Mocks.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(MockCreationBenchmarks).Assembly).Run(args);
