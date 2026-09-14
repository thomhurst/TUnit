# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 344.9 ns   | 140.96 ns | 7.73 ns  | 128 B     |
| Imposter        | 367.6 ns   | 145.62 ns | 7.98 ns  | 168 B     |
| Mockolate       | 120.8 ns   | 45.97 ns  | 2.52 ns  | 84 B      |
| Moq             | 915.9 ns   | 398.30 ns | 21.83 ns | 376 B     |
| NSubstitute     | 817.7 ns   | 268.21 ns | 14.70 ns | 304 B     |
| FakeItEasy      | 2,005.6 ns | 807.14 ns | 44.24 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 189.6 ns   | 148.91 ns | 8.16 ns  | 96 B      |
| Imposter        | 378.2 ns   | 260.66 ns | 14.29 ns | 168 B     |
| Mockolate       | 115.7 ns   | 77.50 ns  | 4.25 ns  | 60 B      |
| Moq             | 627.4 ns   | 143.62 ns | 7.87 ns  | 296 B     |
| NSubstitute     | 712.7 ns   | 294.32 ns | 16.13 ns | 272 B     |
| FakeItEasy      | 1,746.7 ns | 561.16 ns | 30.76 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean         | Error         | StdDev      | Allocated |
| --------------- | ------------ | ------------- | ----------- | --------- |
| **TUnit.Mocks** | 33,720.3 ns  | 21,748.15 ns  | 1,192.09 ns | 13248 B   |
| Imposter        | 36,141.9 ns  | 9,989.70 ns   | 547.57 ns   | 16800 B   |
| Mockolate       | 12,277.7 ns  | 3,271.35 ns   | 179.31 ns   | 8400 B    |
| Moq             | 90,762.2 ns  | 38,440.87 ns  | 2,107.07 ns | 37600 B   |
| NSubstitute     | 77,662.3 ns  | 37,607.48 ns  | 2,061.39 ns | 30848 B   |
| FakeItEasy      | 208,584.9 ns | 110,024.17 ns | 6,030.80 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-14T02:37:23.172Z*
