# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean       | Error     | StdDev    | Allocated |
| --------------- | ---------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 1.811 μs   | 0.0060 μs | 0.0053 μs | 6.23 KB   |
| Imposter        | 2.674 μs   | 0.0063 μs | 0.0056 μs | 15.71 KB  |
| Mockolate       | 1.603 μs   | 0.0079 μs | 0.0066 μs | 7.36 KB   |
| Moq             | 305.400 μs | 2.4009 μs | 2.1283 μs | 36.16 KB  |
| NSubstitute     | 17.070 μs  | 0.0467 μs | 0.0414 μs | 26.72 KB  |
| FakeItEasy      | 15.281 μs  | 0.1552 μs | 0.1452 μs | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-25T02:41:00.074Z*
