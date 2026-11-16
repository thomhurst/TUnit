# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.007 μs   | 0.0122 μs | 0.0108 μs | 6.31 KB   |
| Imposter        | 1.010 μs   | 0.0030 μs | 0.0026 μs | 3.45 KB   |
| Mockolate       | 1.723 μs   | 0.0128 μs | 0.0120 μs | 7.27 KB   |
| Moq             | 199.584 μs | 1.3641 μs | 1.2759 μs | 36.29 KB  |
| NSubstitute     | 18.042 μs  | 0.1639 μs | 0.1369 μs | 26.51 KB  |
| FakeItEasy      | 13.812 μs  | 0.1393 μs | 0.1235 μs | 24.87 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-16T02:25:25.447Z*
