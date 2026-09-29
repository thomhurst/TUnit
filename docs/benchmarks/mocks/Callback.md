# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 654.2 ns     | 2.86 ns   | 2.38 ns   | 3.11 KB   |
| Imposter        | 470.9 ns     | 0.66 ns   | 0.59 ns   | 2.66 KB   |
| Mockolate       | 336.7 ns     | 1.58 ns   | 1.48 ns   | 1.8 KB    |
| Moq             | 136,803.0 ns | 892.52 ns | 834.86 ns | 13.4 KB   |
| NSubstitute     | 4,419.8 ns   | 59.53 ns  | 55.68 ns  | 7.85 KB   |
| FakeItEasy      | 4,571.1 ns   | 51.65 ns  | 43.13 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 770.6 ns     | 4.40 ns   | 3.90 ns   | 3.2 KB    |
| Imposter        | 542.0 ns     | 1.85 ns   | 1.73 ns   | 2.82 KB   |
| Mockolate       | 392.2 ns     | 1.51 ns   | 1.26 ns   | 1.84 KB   |
| Moq             | 141,313.9 ns | 818.47 ns | 683.46 ns | 13.7 KB   |
| NSubstitute     | 5,046.1 ns   | 31.26 ns  | 26.10 ns  | 8.41 KB   |
| FakeItEasy      | 5,491.0 ns   | 51.10 ns  | 47.80 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T02:35:19.410Z*
