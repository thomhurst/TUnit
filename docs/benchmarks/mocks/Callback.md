# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 647.8 ns     | 1.12 ns   | 0.99 ns   | 3.11 KB   |
| Imposter        | 453.4 ns     | 0.54 ns   | 0.48 ns   | 2.66 KB   |
| Mockolate       | 344.3 ns     | 1.87 ns   | 1.75 ns   | 1.8 KB    |
| Moq             | 135,068.8 ns | 680.06 ns | 602.86 ns | 13.29 KB  |
| NSubstitute     | 4,503.9 ns   | 29.21 ns  | 24.39 ns  | 7.85 KB   |
| FakeItEasy      | 4,543.5 ns   | 40.66 ns  | 36.04 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 757.4 ns     | 1.90 ns     | 1.48 ns   | 3.2 KB    |
| Imposter        | 532.1 ns     | 1.25 ns     | 1.11 ns   | 2.82 KB   |
| Mockolate       | 387.8 ns     | 1.90 ns     | 1.78 ns   | 1.84 KB   |
| Moq             | 141,701.3 ns | 1,073.92 ns | 896.77 ns | 13.73 KB  |
| NSubstitute     | 4,964.8 ns   | 18.89 ns    | 16.74 ns  | 8.41 KB   |
| FakeItEasy      | 5,455.2 ns   | 26.03 ns    | 23.07 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-27T04:05:27.840Z*
