# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-13** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 273.67 ns   | 84.15 ns  | 4.612 ns  | 128 B     |
| Imposter        | 293.85 ns   | 78.66 ns  | 4.311 ns  | 168 B     |
| Mockolate       | 108.82 ns   | 52.13 ns  | 2.858 ns  | 84 B      |
| Moq             | 809.95 ns   | 159.28 ns | 8.731 ns  | 376 B     |
| NSubstitute     | 709.05 ns   | 287.34 ns | 15.750 ns | 304 B     |
| FakeItEasy      | 1,732.57 ns | 272.54 ns | 14.939 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 165.67 ns   | 74.65 ns  | 4.092 ns  | 96 B      |
| Imposter        | 302.82 ns   | 76.64 ns  | 4.201 ns  | 168 B     |
| Mockolate       | 94.65 ns    | 25.44 ns  | 1.394 ns  | 60 B      |
| Moq             | 548.76 ns   | 332.55 ns | 18.228 ns | 296 B     |
| NSubstitute     | 618.12 ns   | 142.38 ns | 7.804 ns  | 272 B     |
| FakeItEasy      | 1,590.17 ns | 353.72 ns | 19.388 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 27,414.73 ns  | 10,855.61 ns | 595.033 ns   | 12736 B   |
| Imposter        | 29,509.99 ns  | 15,435.84 ns | 846.090 ns   | 16800 B   |
| Mockolate       | 10,866.83 ns  | 5,406.29 ns  | 296.337 ns   | 8400 B    |
| Moq             | 84,203.67 ns  | 17,807.72 ns | 976.101 ns   | 37600 B   |
| NSubstitute     | 71,268.34 ns  | 21,734.81 ns | 1,191.358 ns | 30848 B   |
| FakeItEasy      | 180,944.79 ns | 81,750.74 ns | 4,481.034 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-13T02:33:28.296Z*
