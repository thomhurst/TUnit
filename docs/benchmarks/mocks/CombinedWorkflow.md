# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.470 μs   | 0.0082 μs | 0.0076 μs | 6.23 KB   |
| Imposter        | 2.018 μs   | 0.0181 μs | 0.0151 μs | 15.71 KB  |
| Mockolate       | 1.346 μs   | 0.0187 μs | 0.0166 μs | 7.36 KB   |
| Moq             | 245.014 μs | 0.9808 μs | 0.7658 μs | 36.3 KB   |
| NSubstitute     | 13.637 μs  | 0.1309 μs | 0.1224 μs | 26.72 KB  |
| FakeItEasy      | 12.532 μs  | 0.1313 μs | 0.1164 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-14T02:37:23.172Z*
