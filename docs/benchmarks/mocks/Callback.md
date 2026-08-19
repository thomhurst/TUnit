# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 530.4 ns     | 10.58 ns  | 11.32 ns  | 3.11 KB   |
| Imposter        | 377.4 ns     | 5.31 ns   | 4.43 ns   | 2.66 KB   |
| Mockolate       | 276.4 ns     | 2.96 ns   | 2.77 ns   | 1.8 KB    |
| Moq             | 107,765.8 ns | 858.24 ns | 760.81 ns | 13.29 KB  |
| NSubstitute     | 3,567.7 ns   | 48.46 ns  | 45.33 ns  | 7.85 KB   |
| FakeItEasy      | 3,843.5 ns   | 58.91 ns  | 55.11 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 627.0 ns     | 6.20 ns   | 5.18 ns   | 3.2 KB    |
| Imposter        | 439.9 ns     | 6.33 ns   | 5.61 ns   | 2.82 KB   |
| Mockolate       | 325.8 ns     | 5.61 ns   | 5.25 ns   | 1.84 KB   |
| Moq             | 116,045.7 ns | 888.12 ns | 787.29 ns | 13.76 KB  |
| NSubstitute     | 4,229.5 ns   | 84.09 ns  | 140.50 ns | 8.41 KB   |
| FakeItEasy      | 4,867.8 ns   | 95.82 ns  | 89.63 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-19T02:42:18.029Z*
