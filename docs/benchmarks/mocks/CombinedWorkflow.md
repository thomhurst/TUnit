# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.965 μs   | 0.0307 μs | 0.0287 μs | 6.23 KB   |
| Imposter        | 2.966 μs   | 0.0440 μs | 0.0412 μs | 15.71 KB  |
| Mockolate       | 1.774 μs   | 0.0343 μs | 0.0321 μs | 7.36 KB   |
| Moq             | 405.519 μs | 1.5824 μs | 1.3214 μs | 36.35 KB  |
| NSubstitute     | 19.296 μs  | 0.1318 μs | 0.1169 μs | 26.72 KB  |
| FakeItEasy      | 18.956 μs  | 0.1987 μs | 0.1551 μs | 25.6 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-27T04:05:27.840Z*
