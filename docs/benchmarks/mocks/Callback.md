# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 341.3 ns     | 4.25 ns   | 3.97 ns   | 1496 B    |
| Imposter        | 195.9 ns     | 1.20 ns   | 1.00 ns   | 848 B     |
| Mockolate       | 177.0 ns     | 3.40 ns   | 3.64 ns   | 832 B     |
| Moq             | 120,465.6 ns | 575.30 ns | 509.99 ns | 13573 B   |
| NSubstitute     | 3,657.4 ns   | 39.71 ns  | 33.16 ns  | 8008 B    |
| FakeItEasy      | 3,853.0 ns   | 48.71 ns  | 45.56 ns  | 7549 B    |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 447.7 ns     | 8.87 ns   | 12.73 ns  | 1592 B    |
| Imposter        | 225.7 ns     | 2.56 ns   | 2.40 ns   | 896 B     |
| Mockolate       | 209.2 ns     | 3.72 ns   | 3.48 ns   | 896 B     |
| Moq             | 128,041.1 ns | 721.13 ns | 639.26 ns | 13987 B   |
| NSubstitute     | 4,019.6 ns   | 77.30 ns  | 72.30 ns  | 8584 B    |
| FakeItEasy      | 4,578.4 ns   | 53.86 ns  | 50.38 ns  | 9624 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-20T02:20:51.427Z*
