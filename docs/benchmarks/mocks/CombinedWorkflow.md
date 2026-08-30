# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.025 μs   | 0.0220 μs | 0.0206 μs | 6.23 KB   |
| Imposter        | 3.133 μs   | 0.0509 μs | 0.0476 μs | 15.71 KB  |
| Mockolate       | 1.892 μs   | 0.0339 μs | 0.0317 μs | 7.36 KB   |
| Moq             | 409.002 μs | 2.4267 μs | 2.0264 μs | 36.27 KB  |
| NSubstitute     | 20.591 μs  | 0.1847 μs | 0.1638 μs | 26.72 KB  |
| FakeItEasy      | 19.889 μs  | 0.1180 μs | 0.1046 μs | 25.74 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-30T02:44:44.759Z*
