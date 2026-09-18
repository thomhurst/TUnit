# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 683.7 ns     | 3.19 ns   | 2.83 ns   | 3.11 KB   |
| Imposter        | 458.5 ns     | 1.86 ns   | 1.74 ns   | 2.66 KB   |
| Mockolate       | 339.7 ns     | 1.49 ns   | 1.39 ns   | 1.8 KB    |
| Moq             | 132,929.9 ns | 638.36 ns | 597.12 ns | 13.29 KB  |
| NSubstitute     | 4,507.2 ns   | 41.64 ns  | 36.92 ns  | 7.85 KB   |
| FakeItEasy      | 4,596.7 ns   | 34.94 ns  | 30.98 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 819.8 ns     | 2.66 ns   | 2.36 ns   | 3.2 KB    |
| Imposter        | 544.7 ns     | 1.56 ns   | 1.46 ns   | 2.82 KB   |
| Mockolate       | 380.5 ns     | 0.82 ns   | 0.72 ns   | 1.84 KB   |
| Moq             | 141,911.6 ns | 469.36 ns | 391.93 ns | 13.75 KB  |
| NSubstitute     | 5,017.9 ns   | 48.40 ns  | 40.42 ns  | 8.41 KB   |
| FakeItEasy      | 5,614.5 ns   | 94.87 ns  | 88.74 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-18T02:32:22.133Z*
