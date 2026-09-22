# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 661.9 ns     | 3.59 ns     | 3.00 ns     | 3.11 KB   |
| Imposter        | 491.2 ns     | 6.60 ns     | 5.85 ns     | 2.66 KB   |
| Mockolate       | 350.2 ns     | 5.66 ns     | 5.01 ns     | 1.8 KB    |
| Moq             | 186,564.4 ns | 1,312.80 ns | 1,227.99 ns | 13.14 KB  |
| NSubstitute     | 4,755.2 ns   | 37.46 ns    | 35.04 ns    | 7.85 KB   |
| FakeItEasy      | 5,341.6 ns   | 73.63 ns    | 68.87 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 787.3 ns     | 6.40 ns     | 5.35 ns     | 3.2 KB    |
| Imposter        | 527.0 ns     | 4.20 ns     | 3.92 ns     | 2.82 KB   |
| Mockolate       | 418.2 ns     | 7.48 ns     | 7.35 ns     | 1.84 KB   |
| Moq             | 194,058.9 ns | 1,237.70 ns | 1,157.75 ns | 13.73 KB  |
| NSubstitute     | 5,684.2 ns   | 59.08 ns    | 55.27 ns    | 8.41 KB   |
| FakeItEasy      | 6,403.4 ns   | 92.06 ns    | 86.11 ns    | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-22T02:33:33.738Z*
