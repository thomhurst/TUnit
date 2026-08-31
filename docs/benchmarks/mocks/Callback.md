# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 752.4 ns     | 6.63 ns     | 6.20 ns     | 3.11 KB   |
| Imposter        | 514.6 ns     | 10.02 ns    | 13.38 ns    | 2.66 KB   |
| Mockolate       | 359.2 ns     | 7.21 ns     | 13.18 ns    | 1.8 KB    |
| Moq             | 184,876.1 ns | 2,480.81 ns | 2,199.17 ns | 13.14 KB  |
| NSubstitute     | 4,930.9 ns   | 62.18 ns    | 58.16 ns    | 7.85 KB   |
| FakeItEasy      | 5,238.7 ns   | 54.82 ns    | 51.28 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 849.7 ns     | 15.08 ns    | 14.11 ns    | 3.2 KB    |
| Imposter        | 582.4 ns     | 11.11 ns    | 10.39 ns    | 2.82 KB   |
| Mockolate       | 434.4 ns     | 8.24 ns     | 8.10 ns     | 1.84 KB   |
| Moq             | 194,564.6 ns | 2,120.37 ns | 1,879.66 ns | 13.73 KB  |
| NSubstitute     | 5,792.7 ns   | 68.88 ns    | 61.06 ns    | 8.41 KB   |
| FakeItEasy      | 6,621.9 ns   | 68.78 ns    | 57.43 ns    | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
