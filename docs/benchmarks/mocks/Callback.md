# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 793.1 ns     | 6.18 ns     | 5.48 ns   | 3.11 KB   |
| Imposter        | 579.9 ns     | 2.50 ns     | 2.22 ns   | 2.66 KB   |
| Mockolate       | 420.2 ns     | 4.09 ns     | 3.82 ns   | 1.8 KB    |
| Moq             | 192,098.6 ns | 1,022.85 ns | 906.73 ns | 13.14 KB  |
| NSubstitute     | 5,270.3 ns   | 23.64 ns    | 20.95 ns  | 7.85 KB   |
| FakeItEasy      | 5,732.7 ns   | 31.82 ns    | 28.20 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 906.6 ns     | 5.73 ns     | 4.79 ns     | 3.2 KB    |
| Imposter        | 639.3 ns     | 5.03 ns     | 4.70 ns     | 2.82 KB   |
| Mockolate       | 488.7 ns     | 4.48 ns     | 4.19 ns     | 1.84 KB   |
| Moq             | 200,465.2 ns | 1,127.08 ns | 1,054.27 ns | 13.73 KB  |
| NSubstitute     | 5,870.0 ns   | 49.70 ns    | 44.05 ns    | 8.41 KB   |
| FakeItEasy      | 6,670.1 ns   | 74.06 ns    | 61.84 ns    | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-30T02:44:44.759Z*
