# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.892 μs   | 0.0295 μs | 0.0276 μs | 6.23 KB   |
| Imposter        | 2.576 μs   | 0.0490 μs | 0.0545 μs | 15.71 KB  |
| Mockolate       | 1.667 μs   | 0.0327 μs | 0.0306 μs | 7.36 KB   |
| Moq             | 309.579 μs | 2.1408 μs | 1.8978 μs | 36.3 KB   |
| NSubstitute     | 18.523 μs  | 0.3638 μs | 0.3573 μs | 26.72 KB  |
| FakeItEasy      | 16.596 μs  | 0.2873 μs | 0.2687 μs | 25.68 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-18T02:39:29.373Z*
