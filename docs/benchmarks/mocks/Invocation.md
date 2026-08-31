# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error      | StdDev    | Allocated |
| --------------- | ----------- | ---------- | --------- | --------- |
| **TUnit.Mocks** | 273.58 ns   | 59.813 ns  | 3.279 ns  | 128 B     |
| Imposter        | 286.95 ns   | 63.711 ns  | 3.492 ns  | 168 B     |
| Mockolate       | 99.59 ns    | 17.918 ns  | 0.982 ns  | 84 B      |
| Moq             | 772.44 ns   | 90.918 ns  | 4.984 ns  | 376 B     |
| NSubstitute     | 696.61 ns   | 107.118 ns | 5.872 ns  | 304 B     |
| FakeItEasy      | 1,723.05 ns | 260.752 ns | 14.293 ns | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error      | StdDev    | Allocated |
| --------------- | ----------- | ---------- | --------- | --------- |
| **TUnit.Mocks** | 165.40 ns   | 86.849 ns  | 4.760 ns  | 96 B      |
| Imposter        | 289.50 ns   | 64.140 ns  | 3.516 ns  | 168 B     |
| Mockolate       | 92.25 ns    | 9.071 ns   | 0.497 ns  | 60 B      |
| Moq             | 512.30 ns   | 129.990 ns | 7.125 ns  | 296 B     |
| NSubstitute     | 626.16 ns   | 104.248 ns | 5.714 ns  | 328 B     |
| FakeItEasy      | 1,496.21 ns | 213.762 ns | 11.717 ns | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error         | StdDev       | Allocated |
| --------------- | ------------- | ------------- | ------------ | --------- |
| **TUnit.Mocks** | 26,628.19 ns  | 10,069.862 ns | 551.963 ns   | 12736 B   |
| Imposter        | 28,306.83 ns  | 9,466.666 ns  | 518.900 ns   | 16800 B   |
| Mockolate       | 9,770.83 ns   | 3,087.244 ns  | 169.222 ns   | 8400 B    |
| Moq             | 78,136.61 ns  | 8,374.136 ns  | 459.015 ns   | 37600 B   |
| NSubstitute     | 70,390.27 ns  | 13,897.552 ns | 761.772 ns   | 30848 B   |
| FakeItEasy      | 167,548.56 ns | 37,629.605 ns | 2,062.606 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
