# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.803 μs   | 0.0100 μs | 0.0083 μs | 6.23 KB   |
| Imposter        | 2.587 μs   | 0.0516 μs | 0.0653 μs | 15.71 KB  |
| Mockolate       | 1.588 μs   | 0.0046 μs | 0.0043 μs | 7.36 KB   |
| Moq             | 407.021 μs | 2.0491 μs | 1.8165 μs | 36.35 KB  |
| NSubstitute     | 18.439 μs  | 0.0779 μs | 0.0690 μs | 26.72 KB  |
| FakeItEasy      | 17.467 μs  | 0.1273 μs | 0.1063 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-26T02:32:00.095Z*
