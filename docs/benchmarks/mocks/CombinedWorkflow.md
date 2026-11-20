# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 1,475.3 ns   | 19.11 ns    | 15.96 ns    | 4.8 KB    |
| Imposter        | 873.6 ns     | 6.63 ns     | 6.20 ns     | 3.45 KB   |
| Mockolate       | 673.1 ns     | 13.05 ns    | 14.51 ns    | 1.96 KB   |
| Moq             | 436,430.4 ns | 1,378.95 ns | 1,151.49 ns | 36.28 KB  |
| NSubstitute     | 18,838.3 ns  | 168.45 ns   | 140.67 ns   | 26.51 KB  |
| FakeItEasy      | 17,010.5 ns  | 99.97 ns    | 93.51 ns    | 24.8 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-20T02:20:51.427Z*
