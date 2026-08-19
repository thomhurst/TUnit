# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.794 μs   | 0.0151 μs | 0.0118 μs | 6.23 KB   |
| Imposter        | 2.624 μs   | 0.0146 μs | 0.0129 μs | 15.71 KB  |
| Mockolate       | 1.608 μs   | 0.0090 μs | 0.0070 μs | 7.36 KB   |
| Moq             | 404.994 μs | 2.7075 μs | 2.2609 μs | 36.54 KB  |
| NSubstitute     | 18.736 μs  | 0.1281 μs | 0.1135 μs | 26.85 KB  |
| FakeItEasy      | 17.817 μs  | 0.1053 μs | 0.0985 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-19T02:42:18.029Z*
