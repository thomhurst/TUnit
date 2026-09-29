# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.497 μs   | 0.0296 μs | 0.0452 μs | 6.23 KB   |
| Imposter        | 2.153 μs   | 0.0424 μs | 0.0697 μs | 15.71 KB  |
| Mockolate       | 1.327 μs   | 0.0245 μs | 0.0416 μs | 7.36 KB   |
| Moq             | 239.839 μs | 1.5880 μs | 1.4854 μs | 36.51 KB  |
| NSubstitute     | 14.083 μs  | 0.1242 μs | 0.1101 μs | 26.72 KB  |
| FakeItEasy      | 13.312 μs  | 0.0833 μs | 0.0779 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T02:35:19.410Z*
