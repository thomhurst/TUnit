using BenchmarkDotNet.Running;
using TUnit.Performance.Tests;

// Run benchmarks
BenchmarkRunner.Run<TestDiscoveryBenchmarks>();
BenchmarkRunner.Run<TestExecutionBenchmarks>();
BenchmarkRunner.Run<DataSourceBenchmarks>();