# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 269.10 ns   | 43.65 ns  | 2.393 ns  | 128 B     |
| Imposter        | 288.28 ns   | 60.26 ns  | 3.303 ns  | 168 B     |
| Mockolate       | 97.12 ns    | 24.99 ns  | 1.370 ns  | 84 B      |
| Moq             | 772.70 ns   | 192.98 ns | 10.578 ns | 376 B     |
| NSubstitute     | 674.66 ns   | 157.29 ns | 8.621 ns  | 304 B     |
| FakeItEasy      | 1,612.88 ns | 248.04 ns | 13.596 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev   | Allocated |
| --------------- | ----------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 167.17 ns   | 62.99 ns  | 3.453 ns | 96 B      |
| Imposter        | 293.44 ns   | 100.74 ns | 5.522 ns | 168 B     |
| Mockolate       | 89.72 ns    | 13.44 ns  | 0.737 ns | 60 B      |
| Moq             | 511.92 ns   | 143.01 ns | 7.839 ns | 296 B     |
| NSubstitute     | 587.19 ns   | 167.15 ns | 9.162 ns | 272 B     |
| FakeItEasy      | 1,466.12 ns | 108.76 ns | 5.962 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 26,710.31 ns  | 8,718.45 ns  | 477.888 ns   | 12736 B   |
| Imposter        | 28,496.53 ns  | 10,044.83 ns | 550.591 ns   | 16800 B   |
| Mockolate       | 9,726.67 ns   | 2,244.99 ns  | 123.056 ns   | 8400 B    |
| Moq             | 77,431.14 ns  | 17,104.16 ns | 937.537 ns   | 37600 B   |
| NSubstitute     | 68,544.95 ns  | 29,665.51 ns | 1,626.067 ns | 30848 B   |
| FakeItEasy      | 171,307.50 ns | 38,179.34 ns | 2,092.739 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-09T02:32:56.707Z*
