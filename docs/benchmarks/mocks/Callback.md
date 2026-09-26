# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 537.1 ns     | 10.56 ns    | 17.65 ns  | 3.11 KB   |
| Imposter        | 377.4 ns     | 3.05 ns     | 2.85 ns   | 2.66 KB   |
| Mockolate       | 271.2 ns     | 4.26 ns     | 3.98 ns   | 1.8 KB    |
| Moq             | 105,889.4 ns | 1,057.10 ns | 988.82 ns | 13.29 KB  |
| NSubstitute     | 3,729.2 ns   | 57.22 ns    | 53.53 ns  | 7.85 KB   |
| FakeItEasy      | 3,697.6 ns   | 72.59 ns    | 94.39 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 654.8 ns     | 4.77 ns     | 4.46 ns   | 3.2 KB    |
| Imposter        | 414.7 ns     | 3.09 ns     | 2.89 ns   | 2.82 KB   |
| Mockolate       | 299.3 ns     | 5.01 ns     | 4.45 ns   | 1.84 KB   |
| Moq             | 114,892.1 ns | 1,020.89 ns | 954.94 ns | 13.72 KB  |
| NSubstitute     | 3,886.7 ns   | 16.16 ns    | 15.12 ns  | 8.41 KB   |
| FakeItEasy      | 4,433.2 ns   | 6.93 ns     | 5.79 ns   | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-26T02:32:00.095Z*
