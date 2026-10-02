# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 641.1 ns     | 2.94 ns     | 2.61 ns     | 3.12 KB   |
| Imposter        | 452.1 ns     | 5.58 ns     | 4.94 ns     | 2.66 KB   |
| Mockolate       | 342.0 ns     | 1.19 ns     | 1.12 ns     | 1.8 KB    |
| Moq             | 185,622.1 ns | 1,352.14 ns | 1,198.63 ns | 13.14 KB  |
| NSubstitute     | 4,972.4 ns   | 11.44 ns    | 10.70 ns    | 7.85 KB   |
| FakeItEasy      | 5,014.5 ns   | 34.01 ns    | 31.82 ns    | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 804.3 ns     | 4.00 ns     | 3.55 ns     | 3.21 KB   |
| Imposter        | 508.3 ns     | 0.86 ns     | 0.81 ns     | 2.82 KB   |
| Mockolate       | 390.0 ns     | 3.77 ns     | 3.53 ns     | 1.84 KB   |
| Moq             | 194,040.4 ns | 1,402.07 ns | 1,311.50 ns | 13.7 KB   |
| NSubstitute     | 5,691.4 ns   | 59.00 ns    | 49.27 ns    | 8.41 KB   |
| FakeItEasy      | 6,425.4 ns   | 71.55 ns    | 63.43 ns    | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-02T02:44:13.542Z*
