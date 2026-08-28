# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 714.1 ns     | 13.53 ns  | 15.04 ns  | 3.11 KB   |
| Imposter        | 490.2 ns     | 3.34 ns   | 3.12 ns   | 2.66 KB   |
| Mockolate       | 373.0 ns     | 4.55 ns   | 4.04 ns   | 1.8 KB    |
| Moq             | 138,376.6 ns | 480.87 ns | 449.80 ns | 13.29 KB  |
| NSubstitute     | 4,842.9 ns   | 15.73 ns  | 14.72 ns  | 7.85 KB   |
| FakeItEasy      | 5,068.7 ns   | 45.27 ns  | 40.13 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 861.7 ns     | 10.40 ns  | 9.72 ns   | 3.2 KB    |
| Imposter        | 564.3 ns     | 5.96 ns   | 5.28 ns   | 2.82 KB   |
| Mockolate       | 408.7 ns     | 4.80 ns   | 4.49 ns   | 1.84 KB   |
| Moq             | 144,570.4 ns | 976.75 ns | 913.66 ns | 13.73 KB  |
| NSubstitute     | 5,277.4 ns   | 102.47 ns | 95.85 ns  | 8.41 KB   |
| FakeItEasy      | 5,966.4 ns   | 88.84 ns  | 83.10 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-28T05:02:48.374Z*
