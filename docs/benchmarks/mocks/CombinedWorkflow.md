# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.919 μs   | 0.0218 μs | 0.0204 μs | 6.23 KB   |
| Imposter        | 2.835 μs   | 0.0425 μs | 0.0398 μs | 15.71 KB  |
| Mockolate       | 1.759 μs   | 0.0145 μs | 0.0136 μs | 7.36 KB   |
| Moq             | 410.270 μs | 2.6115 μs | 2.4428 μs | 36.16 KB  |
| NSubstitute     | 19.877 μs  | 0.2628 μs | 0.2329 μs | 26.85 KB  |
| FakeItEasy      | 19.319 μs  | 0.1706 μs | 0.1513 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-22T02:40:44.558Z*
