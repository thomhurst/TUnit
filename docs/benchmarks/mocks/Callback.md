# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 656.4 ns     | 4.59 ns     | 4.07 ns     | 3.12 KB   |
| Imposter        | 457.6 ns     | 2.19 ns     | 1.83 ns     | 2.64 KB   |
| Mockolate       | 348.0 ns     | 1.67 ns     | 1.48 ns     | 1.8 KB    |
| Moq             | 182,459.3 ns | 1,257.58 ns | 1,114.81 ns | 13.14 KB  |
| NSubstitute     | 4,929.5 ns   | 64.44 ns    | 60.28 ns    | 7.85 KB   |
| FakeItEasy      | 5,164.1 ns   | 32.97 ns    | 30.84 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 793.0 ns     | 5.83 ns     | 4.87 ns     | 3.21 KB   |
| Imposter        | 526.8 ns     | 3.36 ns     | 2.98 ns     | 2.8 KB    |
| Mockolate       | 393.6 ns     | 1.51 ns     | 1.26 ns     | 1.84 KB   |
| Moq             | 202,283.3 ns | 1,070.29 ns | 1,001.15 ns | 13.7 KB   |
| NSubstitute     | 5,607.0 ns   | 13.54 ns    | 11.31 ns    | 8.41 KB   |
| FakeItEasy      | 6,405.0 ns   | 71.25 ns    | 63.16 ns    | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-09T02:42:26.464Z*
