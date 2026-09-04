# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev   | Allocated |
| --------------- | ----------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 276.11 ns   | 61.98 ns  | 3.397 ns | 128 B     |
| Imposter        | 303.36 ns   | 87.10 ns  | 4.774 ns | 168 B     |
| Mockolate       | 120.36 ns   | 50.28 ns  | 2.756 ns | 84 B      |
| Moq             | 813.27 ns   | 76.74 ns  | 4.206 ns | 376 B     |
| NSubstitute     | 710.74 ns   | 172.33 ns | 9.446 ns | 304 B     |
| FakeItEasy      | 1,738.63 ns | 161.03 ns | 8.826 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 167.11 ns   | 74.09 ns  | 4.061 ns  | 96 B      |
| Imposter        | 291.19 ns   | 92.03 ns  | 5.045 ns  | 168 B     |
| Mockolate       | 93.41 ns    | 22.35 ns  | 1.225 ns  | 60 B      |
| Moq             | 532.70 ns   | 81.24 ns  | 4.453 ns  | 296 B     |
| NSubstitute     | 602.82 ns   | 102.70 ns | 5.629 ns  | 272 B     |
| FakeItEasy      | 1,545.15 ns | 591.79 ns | 32.438 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 27,240.28 ns  | 9,886.83 ns  | 541.931 ns   | 12736 B   |
| Imposter        | 29,050.40 ns  | 6,147.02 ns  | 336.939 ns   | 16800 B   |
| Mockolate       | 10,561.82 ns  | 4,525.93 ns  | 248.081 ns   | 8400 B    |
| Moq             | 79,428.50 ns  | 6,454.62 ns  | 353.799 ns   | 37600 B   |
| NSubstitute     | 70,130.53 ns  | 9,730.73 ns  | 533.374 ns   | 30848 B   |
| FakeItEasy      | 173,430.43 ns | 34,832.13 ns | 1,909.267 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-04T02:33:16.366Z*
