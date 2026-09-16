# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.833 μs   | 0.0343 μs | 0.0491 μs | 6.23 KB   |
| Imposter        | 2.329 μs   | 0.0554 μs | 0.1634 μs | 15.71 KB  |
| Mockolate       | 1.489 μs   | 0.0289 μs | 0.0405 μs | 7.36 KB   |
| Moq             | 172.265 μs | 3.0849 μs | 2.8856 μs | 36.19 KB  |
| NSubstitute     | 17.380 μs  | 0.3141 μs | 0.4084 μs | 26.72 KB  |
| FakeItEasy      | 13.204 μs  | 0.2516 μs | 0.2471 μs | 25.51 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-16T02:32:43.042Z*
