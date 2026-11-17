# CombinedWorkflow Benchmark

> Full workflow: create → setup → invoke → verify — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Full workflow: create → setup → invoke → verify:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 1,478.3 ns   | 29.30 ns  | 59.19 ns  | 6.31 KB   |
| Imposter        | 771.7 ns     | 15.28 ns  | 33.55 ns  | 3.45 KB   |
| Mockolate       | 970.1 ns     | 19.36 ns  | 42.10 ns  | 3.9 KB    |
| Moq             | 154,118.8 ns | 782.87 ns | 732.30 ns | 36.53 KB  |
| NSubstitute     | 14,180.8 ns  | 125.88 ns | 117.75 ns | 26.51 KB  |
| FakeItEasy      | 11,047.7 ns  | 181.89 ns | 170.14 ns | 24.79 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for full workflow: create → setup → invoke → verify.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-17T02:23:59.645Z*
