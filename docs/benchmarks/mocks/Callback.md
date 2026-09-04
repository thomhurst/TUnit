# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 525.9 ns     | 10.29 ns  | 10.10 ns  | 3.11 KB   |
| Imposter        | 356.3 ns     | 2.99 ns   | 2.79 ns   | 2.66 KB   |
| Mockolate       | 277.9 ns     | 3.23 ns   | 3.02 ns   | 1.8 KB    |
| Moq             | 107,115.3 ns | 524.39 ns | 490.52 ns | 13.29 KB  |
| NSubstitute     | 3,576.5 ns   | 59.01 ns  | 52.32 ns  | 7.85 KB   |
| FakeItEasy      | 3,780.5 ns   | 28.35 ns  | 23.67 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 600.6 ns     | 3.98 ns   | 3.53 ns   | 3.2 KB    |
| Imposter        | 434.1 ns     | 2.38 ns   | 2.11 ns   | 2.82 KB   |
| Mockolate       | 305.6 ns     | 2.68 ns   | 2.51 ns   | 1.84 KB   |
| Moq             | 114,961.5 ns | 454.49 ns | 402.89 ns | 13.76 KB  |
| NSubstitute     | 3,942.0 ns   | 37.84 ns  | 35.40 ns  | 8.41 KB   |
| FakeItEasy      | 4,552.1 ns   | 61.89 ns  | 54.86 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-04T02:33:16.366Z*
