# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 524.5 ns     | 4.74 ns   | 3.96 ns   | 3.11 KB   |
| Imposter        | 377.2 ns     | 4.79 ns   | 4.48 ns   | 2.66 KB   |
| Mockolate       | 274.2 ns     | 3.07 ns   | 2.87 ns   | 1.8 KB    |
| Moq             | 108,008.1 ns | 672.13 ns | 561.26 ns | 13.29 KB  |
| NSubstitute     | 3,564.6 ns   | 55.53 ns  | 51.95 ns  | 7.85 KB   |
| FakeItEasy      | 3,801.3 ns   | 47.95 ns  | 44.85 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 626.7 ns     | 7.50 ns   | 6.65 ns   | 3.2 KB    |
| Imposter        | 433.0 ns     | 1.87 ns   | 1.66 ns   | 2.82 KB   |
| Mockolate       | 312.6 ns     | 3.49 ns   | 3.27 ns   | 1.84 KB   |
| Moq             | 114,029.5 ns | 664.83 ns | 589.36 ns | 13.76 KB  |
| NSubstitute     | 3,972.7 ns   | 74.00 ns  | 69.22 ns  | 8.41 KB   |
| FakeItEasy      | 4,608.1 ns   | 91.72 ns  | 94.19 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-26T02:57:20.474Z*
