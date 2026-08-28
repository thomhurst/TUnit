# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 2.010 μs   | 0.0140 μs | 0.0131 μs | 6.23 KB   |
| Imposter        | 2.997 μs   | 0.0376 μs | 0.0352 μs | 15.71 KB  |
| Mockolate       | 1.805 μs   | 0.0208 μs | 0.0194 μs | 7.36 KB   |
| Moq             | 410.603 μs | 1.5688 μs | 1.4675 μs | 36.16 KB  |
| NSubstitute     | 19.615 μs  | 0.0992 μs | 0.0879 μs | 26.72 KB  |
| FakeItEasy      | 18.916 μs  | 0.2952 μs | 0.2617 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-28T05:02:48.374Z*
