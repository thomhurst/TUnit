# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.804 μs   | 0.0102 μs | 0.0096 μs | 6.23 KB   |
| Imposter        | 2.547 μs   | 0.0068 μs | 0.0060 μs | 15.71 KB  |
| Mockolate       | 1.577 μs   | 0.0071 μs | 0.0063 μs | 7.36 KB   |
| Moq             | 405.830 μs | 1.2034 μs | 1.0668 μs | 36.53 KB  |
| NSubstitute     | 18.334 μs  | 0.0458 μs | 0.0357 μs | 26.72 KB  |
| FakeItEasy      | 17.983 μs  | 0.0680 μs | 0.0636 μs | 25.74 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-20T02:41:11.657Z*
