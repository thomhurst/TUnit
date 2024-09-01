using BenchmarkDotNet.Running;
using Tests.Benchmarking;

var summary = BenchmarkRunner.Run<Benchmarks>();