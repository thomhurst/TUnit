# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 543.9 ns     | 10.55 ns  | 12.15 ns  | 3200 B    |
| Imposter        | 204.8 ns     | 3.54 ns   | 3.31 ns   | 848 B     |
| Mockolate       | 256.3 ns     | 2.77 ns   | 2.31 ns   | 1824 B    |
| Moq             | 119,810.5 ns | 505.39 ns | 422.02 ns | 13573 B   |
| NSubstitute     | 3,604.0 ns   | 70.53 ns  | 72.43 ns  | 8008 B    |
| FakeItEasy      | 3,756.9 ns   | 35.18 ns  | 29.38 ns  | 7549 B    |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 642.6 ns     | 7.98 ns   | 7.07 ns   | 3296 B    |
| Imposter        | 242.0 ns     | 2.74 ns   | 2.43 ns   | 896 B     |
| Mockolate       | 337.2 ns     | 3.96 ns   | 3.70 ns   | 1872 B    |
| Moq             | 122,974.8 ns | 454.27 ns | 354.66 ns | 13987 B   |
| NSubstitute     | 4,081.3 ns   | 74.93 ns  | 70.09 ns  | 8584 B    |
| FakeItEasy      | 4,577.6 ns   | 90.58 ns  | 104.31 ns | 9624 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-13T02:20:37.421Z*
