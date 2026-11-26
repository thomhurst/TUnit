# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 285.5 ns     | 2.50 ns   | 2.34 ns   | 1216 B    |
| Imposter        | 206.1 ns     | 3.70 ns   | 4.55 ns   | 848 B     |
| Mockolate       | 174.7 ns     | 1.57 ns   | 1.46 ns   | 832 B     |
| Moq             | 122,591.3 ns | 911.07 ns | 807.64 ns | 13519 B   |
| NSubstitute     | 3,597.2 ns   | 71.44 ns  | 92.90 ns  | 8008 B    |
| FakeItEasy      | 3,572.5 ns   | 20.35 ns  | 18.04 ns  | 7549 B    |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 407.8 ns     | 8.14 ns   | 11.41 ns  | 1400 B    |
| Imposter        | 223.0 ns     | 1.50 ns   | 1.41 ns   | 896 B     |
| Mockolate       | 211.2 ns     | 4.15 ns   | 5.25 ns   | 896 B     |
| Moq             | 124,397.2 ns | 499.06 ns | 416.74 ns | 13987 B   |
| NSubstitute     | 3,896.5 ns   | 40.11 ns  | 37.52 ns  | 8584 B    |
| FakeItEasy      | 4,459.0 ns   | 47.24 ns  | 44.19 ns  | 9624 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-26T02:21:09.958Z*
