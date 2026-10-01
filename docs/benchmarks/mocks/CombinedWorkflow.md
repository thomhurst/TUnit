# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.812 μs   | 0.0288 μs | 0.0269 μs | 6.28 KB   |
| Imposter        | 2.554 μs   | 0.0448 μs | 0.0397 μs | 15.71 KB  |
| Mockolate       | 1.587 μs   | 0.0194 μs | 0.0162 μs | 7.36 KB   |
| Moq             | 404.897 μs | 2.4588 μs | 2.1797 μs | 36.3 KB   |
| NSubstitute     | 18.618 μs  | 0.0821 μs | 0.0728 μs | 26.72 KB  |
| FakeItEasy      | 18.261 μs  | 0.0942 μs | 0.0787 μs | 25.63 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-01T02:49:56.169Z*
