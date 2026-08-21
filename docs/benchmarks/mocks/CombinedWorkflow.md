# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.438 μs   | 0.0097 μs | 0.0086 μs | 6.23 KB   |
| Imposter        | 2.065 μs   | 0.0136 μs | 0.0114 μs | 15.71 KB  |
| Mockolate       | 1.339 μs   | 0.0267 μs | 0.0591 μs | 7.36 KB   |
| Moq             | 242.209 μs | 1.7154 μs | 1.5206 μs | 36.46 KB  |
| NSubstitute     | 13.449 μs  | 0.1827 μs | 0.1620 μs | 26.72 KB  |
| FakeItEasy      | 12.234 μs  | 0.1206 μs | 0.1128 μs | 25.63 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-21T02:46:27.792Z*
