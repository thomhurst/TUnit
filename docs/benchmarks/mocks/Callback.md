# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 640.6 ns     | 2.64 ns   | 2.34 ns   | 3.13 KB   |
| Imposter        | 458.7 ns     | 3.12 ns   | 2.92 ns   | 2.61 KB   |
| Mockolate       | 339.2 ns     | 1.24 ns   | 1.10 ns   | 1.8 KB    |
| Moq             | 185,174.8 ns | 834.73 ns | 697.04 ns | 13.14 KB  |
| NSubstitute     | 4,869.3 ns   | 22.45 ns  | 18.75 ns  | 7.85 KB   |
| FakeItEasy      | 5,214.4 ns   | 45.43 ns  | 42.50 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 826.2 ns     | 4.75 ns     | 4.44 ns     | 3.23 KB   |
| Imposter        | 538.6 ns     | 5.05 ns     | 4.73 ns     | 2.77 KB   |
| Mockolate       | 393.7 ns     | 1.91 ns     | 1.79 ns     | 1.84 KB   |
| Moq             | 195,580.9 ns | 1,191.87 ns | 1,056.56 ns | 13.7 KB   |
| NSubstitute     | 5,512.7 ns   | 49.46 ns    | 43.85 ns    | 8.41 KB   |
| FakeItEasy      | 6,257.2 ns   | 68.23 ns    | 56.98 ns    | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-16T03:11:30.127Z*
