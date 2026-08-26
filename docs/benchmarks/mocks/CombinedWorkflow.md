# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.005 μs   | 0.0324 μs | 0.0287 μs | 6.23 KB   |
| Imposter        | 2.783 μs   | 0.0556 μs | 0.1002 μs | 15.71 KB  |
| Mockolate       | 1.790 μs   | 0.0342 μs | 0.0380 μs | 7.36 KB   |
| Moq             | 303.325 μs | 3.7601 μs | 3.3332 μs | 36.3 KB   |
| NSubstitute     | 18.320 μs  | 0.1262 μs | 0.1180 μs | 26.72 KB  |
| FakeItEasy      | 16.434 μs  | 0.2877 μs | 0.2550 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-26T02:57:20.474Z*
