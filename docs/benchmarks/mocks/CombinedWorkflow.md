# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.660 μs   | 0.0184 μs | 0.0163 μs | 6.31 KB   |
| Imposter        | 2.295 μs   | 0.0458 μs | 0.1124 μs | 15.42 KB  |
| Mockolate       | 1.531 μs   | 0.0306 μs | 0.0300 μs | 7.36 KB   |
| Moq             | 245.709 μs | 1.6205 μs | 1.4365 μs | 36.6 KB   |
| NSubstitute     | 14.555 μs  | 0.0976 μs | 0.0815 μs | 26.72 KB  |
| FakeItEasy      | 14.061 μs  | 0.0677 μs | 0.0633 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-16T03:11:30.127Z*
