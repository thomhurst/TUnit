# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 270.29 ns   | 86.17 ns  | 4.723 ns  | 128 B     |
| Imposter        | 295.83 ns   | 53.48 ns  | 2.931 ns  | 168 B     |
| Mockolate       | 103.37 ns   | 86.35 ns  | 4.733 ns  | 84 B      |
| Moq             | 803.05 ns   | 651.63 ns | 35.718 ns | 376 B     |
| NSubstitute     | 755.36 ns   | 34.24 ns  | 1.877 ns  | 360 B     |
| FakeItEasy      | 1,780.24 ns | 633.21 ns | 34.708 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 167.15 ns   | 68.77 ns  | 3.770 ns  | 96 B      |
| Imposter        | 291.19 ns   | 77.15 ns  | 4.229 ns  | 168 B     |
| Mockolate       | 92.76 ns    | 55.51 ns  | 3.043 ns  | 60 B      |
| Moq             | 543.19 ns   | 91.53 ns  | 5.017 ns  | 296 B     |
| NSubstitute     | 636.18 ns   | 313.84 ns | 17.203 ns | 272 B     |
| FakeItEasy      | 1,574.66 ns | 433.66 ns | 23.771 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 26,996.11 ns  | 6,993.61 ns  | 383.343 ns   | 12736 B   |
| Imposter        | 28,842.66 ns  | 11,322.70 ns | 620.635 ns   | 16800 B   |
| Mockolate       | 10,112.98 ns  | 1,385.39 ns  | 75.938 ns    | 8400 B    |
| Moq             | 82,115.24 ns  | 39,472.71 ns | 2,163.632 ns | 37600 B   |
| NSubstitute     | 72,020.19 ns  | 6,652.60 ns  | 364.651 ns   | 30848 B   |
| FakeItEasy      | 170,031.55 ns | 39,095.72 ns | 2,142.968 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-04T03:10:54.658Z*
