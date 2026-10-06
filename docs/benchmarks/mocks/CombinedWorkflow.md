# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.516 μs   | 0.0146 μs | 0.0114 μs | 6.28 KB   |
| Imposter        | 1.939 μs   | 0.0378 μs | 0.0491 μs | 15.71 KB  |
| Mockolate       | 1.239 μs   | 0.0203 μs | 0.0180 μs | 7.36 KB   |
| Moq             | 239.608 μs | 1.8675 μs | 1.7469 μs | 36.35 KB  |
| NSubstitute     | 13.300 μs  | 0.2565 μs | 0.2634 μs | 26.85 KB  |
| FakeItEasy      | 12.529 μs  | 0.2195 μs | 0.2054 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-06T02:37:20.591Z*
