# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-15** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 283.1 ns   | 89.17 ns  | 4.89 ns  | 128 B     |
| Imposter        | 306.5 ns   | 74.36 ns  | 4.08 ns  | 168 B     |
| Mockolate       | 123.4 ns   | 22.48 ns  | 1.23 ns  | 84 B      |
| Moq             | 863.1 ns   | 281.12 ns | 15.41 ns | 376 B     |
| NSubstitute     | 772.1 ns   | 166.22 ns | 9.11 ns  | 304 B     |
| FakeItEasy      | 1,929.6 ns | 508.64 ns | 27.88 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean       | Error     | StdDev   | Allocated |
| --------------- | ---------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 168.6 ns   | 54.28 ns  | 2.98 ns  | 96 B      |
| Imposter        | 308.6 ns   | 84.66 ns  | 4.64 ns  | 168 B     |
| Mockolate       | 111.5 ns   | 64.02 ns  | 3.51 ns  | 60 B      |
| Moq             | 576.7 ns   | 187.04 ns | 10.25 ns | 296 B     |
| NSubstitute     | 684.3 ns   | 197.88 ns | 10.85 ns | 272 B     |
| FakeItEasy      | 1,586.4 ns | 380.07 ns | 20.83 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean         | Error        | StdDev      | Allocated |
| --------------- | ------------ | ------------ | ----------- | --------- |
| **TUnit.Mocks** | 27,360.0 ns  | 15,169.20 ns | 831.47 ns   | 12736 B   |
| Imposter        | 30,120.1 ns  | 11,255.75 ns | 616.97 ns   | 16800 B   |
| Mockolate       | 11,402.9 ns  | 6,564.79 ns  | 359.84 ns   | 8400 B    |
| Moq             | 86,067.1 ns  | 45,572.78 ns | 2,498.00 ns | 37600 B   |
| NSubstitute     | 75,180.6 ns  | 76,511.21 ns | 4,193.84 ns | 30848 B   |
| FakeItEasy      | 186,544.5 ns | 60,044.80 ns | 3,291.26 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-15T02:33:23.208Z*
