# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-03** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 666.0 ns     | 9.28 ns   | 8.68 ns   | 3.11 KB   |
| Imposter        | 466.9 ns     | 4.33 ns   | 3.84 ns   | 2.66 KB   |
| Mockolate       | 388.3 ns     | 2.60 ns   | 2.43 ns   | 1.8 KB    |
| Moq             | 132,427.3 ns | 789.54 ns | 659.30 ns | 13.14 KB  |
| NSubstitute     | 4,579.7 ns   | 52.91 ns  | 49.49 ns  | 7.85 KB   |
| FakeItEasy      | 4,848.1 ns   | 64.53 ns  | 60.37 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 812.3 ns     | 8.46 ns   | 7.92 ns   | 3.2 KB    |
| Imposter        | 534.4 ns     | 5.63 ns   | 5.27 ns   | 2.82 KB   |
| Mockolate       | 391.2 ns     | 2.25 ns   | 1.88 ns   | 1.84 KB   |
| Moq             | 144,041.0 ns | 934.10 ns | 780.02 ns | 13.73 KB  |
| NSubstitute     | 5,154.5 ns   | 90.56 ns  | 84.71 ns  | 8.41 KB   |
| FakeItEasy      | 5,663.1 ns   | 59.34 ns  | 55.50 ns  | 9.4 KB    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-03T02:45:05.205Z*
