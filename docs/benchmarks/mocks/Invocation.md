# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 272.45 ns   | 75.77 ns  | 4.153 ns  | 128 B     |
| Imposter        | 290.68 ns   | 63.53 ns  | 3.482 ns  | 168 B     |
| Mockolate       | 107.48 ns   | 71.94 ns  | 3.943 ns  | 84 B      |
| Moq             | 825.07 ns   | 272.41 ns | 14.932 ns | 376 B     |
| NSubstitute     | 707.78 ns   | 193.02 ns | 10.580 ns | 304 B     |
| FakeItEasy      | 1,734.81 ns | 204.30 ns | 11.198 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 166.51 ns   | 72.61 ns  | 3.980 ns  | 96 B      |
| Imposter        | 291.14 ns   | 88.90 ns  | 4.873 ns  | 168 B     |
| Mockolate       | 97.53 ns    | 49.61 ns  | 2.719 ns  | 60 B      |
| Moq             | 543.50 ns   | 245.10 ns | 13.435 ns | 296 B     |
| NSubstitute     | 610.89 ns   | 202.49 ns | 11.099 ns | 272 B     |
| FakeItEasy      | 1,545.20 ns | 112.26 ns | 6.153 ns  | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 27,119.77 ns  | 7,353.58 ns  | 403.075 ns   | 12736 B   |
| Imposter        | 29,375.44 ns  | 8,038.29 ns  | 440.606 ns   | 16800 B   |
| Mockolate       | 10,307.65 ns  | 3,281.68 ns  | 179.880 ns   | 8400 B    |
| Moq             | 80,072.10 ns  | 8,608.29 ns  | 471.850 ns   | 37600 B   |
| NSubstitute     | 73,798.98 ns  | 13,640.65 ns | 747.690 ns   | 30848 B   |
| FakeItEasy      | 180,604.71 ns | 65,636.90 ns | 3,597.780 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-24T02:46:06.016Z*
