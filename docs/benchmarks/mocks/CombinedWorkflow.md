# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 1,030.9 ns   | 16.93 ns  | 14.14 ns  | 6.28 KB   |
| Imposter        | 1,300.3 ns   | 8.22 ns   | 6.87 ns   | 15.71 KB  |
| Mockolate       | 844.6 ns     | 7.86 ns   | 7.35 ns   | 7.36 KB   |
| Moq             | 115,121.8 ns | 870.55 ns | 814.31 ns | 36.36 KB  |
| NSubstitute     | 8,958.3 ns   | 107.01 ns | 94.86 ns  | 26.89 KB  |
| FakeItEasy      | 7,934.2 ns   | 85.38 ns  | 71.30 ns  | 25.52 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-03T02:34:47.768Z*
