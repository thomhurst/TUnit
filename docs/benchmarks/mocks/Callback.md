# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 615.9 ns    | 11.56 ns  | 10.81 ns  | 3200 B    |
| Imposter        | 238.4 ns    | 2.49 ns   | 2.33 ns   | 848 B     |
| Mockolate       | 405.2 ns    | 4.14 ns   | 3.87 ns   | 1744 B    |
| Moq             | 72,457.3 ns | 251.80 ns | 235.53 ns | 13565 B   |
| NSubstitute     | 3,626.6 ns  | 42.59 ns  | 39.84 ns  | 8008 B    |
| FakeItEasy      | 3,250.4 ns  | 24.06 ns  | 21.33 ns  | 7542 B    |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 690.7 ns    | 13.76 ns  | 20.60 ns  | 3296 B    |
| Imposter        | 258.8 ns    | 3.05 ns   | 2.85 ns   | 896 B     |
| Mockolate       | 331.1 ns    | 6.08 ns   | 6.76 ns   | 1744 B    |
| Moq             | 76,220.1 ns | 443.85 ns | 415.18 ns | 13980 B   |
| NSubstitute     | 3,794.6 ns  | 73.21 ns  | 100.21 ns | 8584 B    |
| FakeItEasy      | 3,751.6 ns  | 71.28 ns  | 79.23 ns  | 9638 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-19T02:23:19.275Z*
