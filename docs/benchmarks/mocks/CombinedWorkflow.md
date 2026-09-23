# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.129 μs   | 0.0333 μs | 0.0312 μs | 6.23 KB   |
| Imposter        | 3.051 μs   | 0.0605 μs | 0.0867 μs | 15.71 KB  |
| Mockolate       | 1.834 μs   | 0.0353 μs | 0.0331 μs | 7.36 KB   |
| Moq             | 410.226 μs | 1.4248 μs | 1.3328 μs | 36.49 KB  |
| NSubstitute     | 19.563 μs  | 0.2605 μs | 0.2309 μs | 26.72 KB  |
| FakeItEasy      | 19.125 μs  | 0.1836 μs | 0.1718 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-23T02:34:56.618Z*
