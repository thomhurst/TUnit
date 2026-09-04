# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.890 μs   | 0.0200 μs | 0.0187 μs | 6.23 KB   |
| Imposter        | 2.892 μs   | 0.0578 μs | 0.0540 μs | 15.71 KB  |
| Mockolate       | 1.680 μs   | 0.0194 μs | 0.0172 μs | 7.36 KB   |
| Moq             | 404.656 μs | 2.0986 μs | 1.8603 μs | 36.49 KB  |
| NSubstitute     | 19.260 μs  | 0.0823 μs | 0.0770 μs | 26.72 KB  |
| FakeItEasy      | 19.347 μs  | 0.1678 μs | 0.1488 μs | 25.85 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-04T02:33:16.366Z*
