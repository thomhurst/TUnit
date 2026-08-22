# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 269.03 ns   | 85.12 ns  | 4.666 ns  | 128 B     |
| Imposter        | 302.37 ns   | 78.56 ns  | 4.306 ns  | 168 B     |
| Mockolate       | 105.95 ns   | 24.82 ns  | 1.360 ns  | 84 B      |
| Moq             | 775.37 ns   | 220.28 ns | 12.074 ns | 376 B     |
| NSubstitute     | 716.38 ns   | 247.07 ns | 13.543 ns | 304 B     |
| FakeItEasy      | 1,711.45 ns | 255.21 ns | 13.989 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 175.33 ns   | 56.99 ns  | 3.124 ns  | 96 B      |
| Imposter        | 298.80 ns   | 78.14 ns  | 4.283 ns  | 168 B     |
| Mockolate       | 96.88 ns    | 22.20 ns  | 1.217 ns  | 60 B      |
| Moq             | 528.23 ns   | 112.55 ns | 6.169 ns  | 296 B     |
| NSubstitute     | 621.74 ns   | 233.66 ns | 12.808 ns | 272 B     |
| FakeItEasy      | 1,541.55 ns | 115.31 ns | 6.320 ns  | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 26,812.18 ns  | 10,983.27 ns | 602.030 ns   | 12736 B   |
| Imposter        | 29,466.17 ns  | 8,702.29 ns  | 477.002 ns   | 16800 B   |
| Mockolate       | 10,551.18 ns  | 1,999.80 ns  | 109.616 ns   | 8400 B    |
| Moq             | 78,241.31 ns  | 12,304.04 ns | 674.426 ns   | 37600 B   |
| NSubstitute     | 70,529.92 ns  | 34,670.98 ns | 1,900.433 ns | 30848 B   |
| FakeItEasy      | 172,471.85 ns | 39,238.35 ns | 2,150.786 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-22T02:40:44.558Z*
