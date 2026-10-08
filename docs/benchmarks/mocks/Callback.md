# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 720.9 ns     | 10.99 ns    | 10.28 ns    | 3.12 KB   |
| Imposter        | 509.8 ns     | 10.18 ns    | 11.32 ns    | 2.64 KB   |
| Mockolate       | 378.0 ns     | 7.02 ns     | 6.57 ns     | 1.8 KB    |
| Moq             | 187,963.7 ns | 1,117.10 ns | 1,044.93 ns | 13.26 KB  |
| NSubstitute     | 5,122.6 ns   | 30.03 ns    | 26.62 ns    | 7.85 KB   |
| FakeItEasy      | 5,510.6 ns   | 69.23 ns    | 64.76 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 860.3 ns     | 12.82 ns  | 11.37 ns  | 3.21 KB   |
| Imposter        | 683.5 ns     | 12.30 ns  | 11.51 ns  | 2.8 KB    |
| Mockolate       | 421.4 ns     | 8.21 ns   | 8.78 ns   | 1.84 KB   |
| Moq             | 191,916.0 ns | 786.97 ns | 736.13 ns | 13.7 KB   |
| NSubstitute     | 5,938.7 ns   | 66.76 ns  | 62.45 ns  | 8.41 KB   |
| FakeItEasy      | 6,562.4 ns   | 88.70 ns  | 78.63 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-08T02:41:51.713Z*
