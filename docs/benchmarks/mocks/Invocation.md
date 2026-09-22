# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean       | Error       | StdDev    | Allocated |
| --------------- | ---------- | ----------- | --------- | --------- |
| **TUnit.Mocks** | 288.8 ns   | 64.23 ns    | 3.52 ns   | 128 B     |
| Imposter        | 304.9 ns   | 75.63 ns    | 4.15 ns   | 168 B     |
| Mockolate       | 124.8 ns   | 107.51 ns   | 5.89 ns   | 84 B      |
| Moq             | 865.1 ns   | 338.31 ns   | 18.54 ns  | 376 B     |
| NSubstitute     | 812.1 ns   | 233.56 ns   | 12.80 ns  | 360 B     |
| FakeItEasy      | 1,872.3 ns | 1,836.57 ns | 100.67 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 167.4 ns   | 66.04 ns  | 3.62 ns  | 96 B      |
| Imposter        | 307.7 ns   | 24.66 ns  | 1.35 ns  | 168 B     |
| Mockolate       | 106.2 ns   | 53.12 ns  | 2.91 ns  | 60 B      |
| Moq             | 571.2 ns   | 81.87 ns  | 4.49 ns  | 296 B     |
| NSubstitute     | 642.2 ns   | 341.20 ns | 18.70 ns | 272 B     |
| FakeItEasy      | 1,641.4 ns | 102.00 ns | 5.59 ns  | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean         | Error        | StdDev      | Allocated |
| --------------- | ------------ | ------------ | ----------- | --------- |
| **TUnit.Mocks** | 28,422.4 ns  | 8,276.11 ns  | 453.64 ns   | 12736 B   |
| Imposter        | 30,252.2 ns  | 5,403.26 ns  | 296.17 ns   | 16800 B   |
| Mockolate       | 11,762.3 ns  | 1,486.48 ns  | 81.48 ns    | 8400 B    |
| Moq             | 80,885.5 ns  | 15,604.50 ns | 855.34 ns   | 37600 B   |
| NSubstitute     | 73,991.9 ns  | 32,134.57 ns | 1,761.40 ns | 30848 B   |
| FakeItEasy      | 191,024.1 ns | 83,236.88 ns | 4,562.49 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-22T02:33:33.738Z*
