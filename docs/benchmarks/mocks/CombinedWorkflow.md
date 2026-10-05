# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.861 μs   | 0.0133 μs | 0.0111 μs | 6.28 KB   |
| Imposter        | 2.590 μs   | 0.0159 μs | 0.0149 μs | 15.71 KB  |
| Mockolate       | 1.638 μs   | 0.0298 μs | 0.0279 μs | 7.36 KB   |
| Moq             | 407.801 μs | 3.6061 μs | 3.3731 μs | 36.16 KB  |
| NSubstitute     | 19.276 μs  | 0.0897 μs | 0.0839 μs | 26.72 KB  |
| FakeItEasy      | 18.479 μs  | 0.1791 μs | 0.1676 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-05T02:45:14.260Z*
