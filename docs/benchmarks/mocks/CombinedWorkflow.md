# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.558 μs   | 0.0176 μs | 0.0164 μs | 6.23 KB   |
| Imposter        | 2.044 μs   | 0.0137 μs | 0.0121 μs | 15.71 KB  |
| Mockolate       | 1.340 μs   | 0.0165 μs | 0.0155 μs | 7.36 KB   |
| Moq             | 165.445 μs | 0.9659 μs | 0.8562 μs | 36.08 KB  |
| NSubstitute     | 15.396 μs  | 0.1188 μs | 0.0992 μs | 26.72 KB  |
| FakeItEasy      | 12.136 μs  | 0.1492 μs | 0.1396 μs | 25.62 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-23T02:45:27.613Z*
