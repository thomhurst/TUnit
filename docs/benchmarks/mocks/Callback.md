# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 526.1 ns     | 3.14 ns   | 2.94 ns   | 3.11 KB   |
| Imposter        | 357.7 ns     | 1.20 ns   | 1.12 ns   | 2.66 KB   |
| Mockolate       | 266.7 ns     | 1.48 ns   | 1.31 ns   | 1.8 KB    |
| Moq             | 105,106.3 ns | 512.04 ns | 399.77 ns | 13.29 KB  |
| NSubstitute     | 3,433.5 ns   | 28.25 ns  | 23.59 ns  | 7.85 KB   |
| FakeItEasy      | 3,735.9 ns   | 29.36 ns  | 27.46 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 599.9 ns     | 3.08 ns   | 2.88 ns   | 3.2 KB    |
| Imposter        | 414.0 ns     | 0.88 ns   | 0.69 ns   | 2.82 KB   |
| Mockolate       | 304.8 ns     | 0.71 ns   | 0.63 ns   | 1.84 KB   |
| Moq             | 112,273.8 ns | 585.37 ns | 488.81 ns | 13.76 KB  |
| NSubstitute     | 3,942.5 ns   | 25.63 ns  | 22.72 ns  | 8.41 KB   |
| FakeItEasy      | 4,349.1 ns   | 16.12 ns  | 13.46 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T02:43:20.076Z*
