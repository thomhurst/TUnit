# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.811 μs   | 0.0039 μs | 0.0033 μs | 6.23 KB   |
| Imposter        | 2.458 μs   | 0.0070 μs | 0.0059 μs | 15.71 KB  |
| Mockolate       | 1.573 μs   | 0.0047 μs | 0.0041 μs | 7.36 KB   |
| Moq             | 313.980 μs | 1.8489 μs | 1.6390 μs | 36.27 KB  |
| NSubstitute     | 17.087 μs  | 0.1570 μs | 0.1392 μs | 26.72 KB  |
| FakeItEasy      | 15.380 μs  | 0.1391 μs | 0.1301 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-08T02:32:39.573Z*
