# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.956 μs   | 0.0200 μs | 0.0187 μs | 6.23 KB   |
| Imposter        | 2.684 μs   | 0.0311 μs | 0.0291 μs | 15.71 KB  |
| Mockolate       | 1.663 μs   | 0.0167 μs | 0.0148 μs | 7.36 KB   |
| Moq             | 406.492 μs | 1.6243 μs | 1.3564 μs | 36.46 KB  |
| NSubstitute     | 19.215 μs  | 0.1667 μs | 0.1559 μs | 26.72 KB  |
| FakeItEasy      | 17.813 μs  | 0.2340 μs | 0.2189 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-24T02:46:06.016Z*
