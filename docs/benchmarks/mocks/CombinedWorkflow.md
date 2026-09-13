# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.861 μs   | 0.0370 μs | 0.0309 μs | 6.23 KB   |
| Imposter        | 2.820 μs   | 0.0319 μs | 0.0298 μs | 15.71 KB  |
| Mockolate       | 1.651 μs   | 0.0224 μs | 0.0198 μs | 7.36 KB   |
| Moq             | 416.108 μs | 3.1980 μs | 2.8349 μs | 36.49 KB  |
| NSubstitute     | 18.775 μs  | 0.1301 μs | 0.1217 μs | 26.72 KB  |
| FakeItEasy      | 18.351 μs  | 0.2654 μs | 0.2483 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-13T02:33:28.296Z*
