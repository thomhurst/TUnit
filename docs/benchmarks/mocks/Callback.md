# Callback Benchmark

> Callback registration and execution — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Callback registration and execution:

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 708.8 ns     | 9.69 ns   | 9.07 ns   | 3.11 KB   |
| Imposter        | 489.7 ns     | 9.73 ns   | 15.15 ns  | 2.66 KB   |
| Mockolate       | 388.1 ns     | 7.75 ns   | 8.61 ns   | 1.8 KB    |
| Moq             | 185,881.1 ns | 758.35 ns | 672.26 ns | 13.14 KB  |
| NSubstitute     | 4,993.8 ns   | 55.27 ns  | 51.70 ns  | 7.85 KB   |
| FakeItEasy      | 5,533.6 ns   | 75.50 ns  | 66.93 ns  | 7.44 KB   |

<!-- -->

***

### with args[​](#with-args "Direct link to with args")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 807.5 ns     | 16.17 ns  | 19.86 ns  | 3.2 KB    |
| Imposter        | 547.1 ns     | 10.95 ns  | 12.61 ns  | 2.82 KB   |
| Mockolate       | 444.7 ns     | 8.88 ns   | 10.22 ns  | 1.84 KB   |
| Moq             | 197,985.6 ns | 946.48 ns | 839.03 ns | 13.7 KB   |
| NSubstitute     | 5,714.8 ns   | 39.38 ns  | 34.91 ns  | 8.41 KB   |
| FakeItEasy      | 6,744.8 ns   | 93.52 ns  | 82.90 ns  | 9.26 KB   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for callback registration and execution.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-25T02:32:28.119Z*
