# Invocation Benchmark

> Calling methods on mock objects — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-25** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Calling methods on mock objects:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 272.78 ns   | 75.34 ns  | 4.129 ns  | 128 B     |
| Imposter        | 300.07 ns   | 66.55 ns  | 3.648 ns  | 168 B     |
| Mockolate       | 107.55 ns   | 72.98 ns  | 4.000 ns  | 84 B      |
| Moq             | 825.75 ns   | 404.12 ns | 22.151 ns | 376 B     |
| NSubstitute     | 729.80 ns   | 302.15 ns | 16.562 ns | 304 B     |
| FakeItEasy      | 1,770.83 ns | 102.11 ns | 5.597 ns  | 944 B     |

<!-- -->

***

### String[​](#string "Direct link to String")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 165.24 ns   | 65.11 ns  | 3.569 ns  | 96 B      |
| Imposter        | 295.07 ns   | 60.44 ns  | 3.313 ns  | 168 B     |
| Mockolate       | 92.44 ns    | 14.06 ns  | 0.771 ns  | 60 B      |
| Moq             | 522.96 ns   | 130.91 ns | 7.175 ns  | 296 B     |
| NSubstitute     | 605.24 ns   | 325.82 ns | 17.859 ns | 272 B     |
| FakeItEasy      | 1,522.53 ns | 121.11 ns | 6.639 ns  | 776 B     |

<!-- -->

***

### 100 calls[​](#100-calls "Direct link to 100 calls")

| Library         | Mean          | Error        | StdDev       | Allocated |
| --------------- | ------------- | ------------ | ------------ | --------- |
| **TUnit.Mocks** | 27,216.40 ns  | 11,592.68 ns | 635.434 ns   | 12736 B   |
| Imposter        | 29,442.91 ns  | 14,466.88 ns | 792.978 ns   | 16800 B   |
| Mockolate       | 10,167.98 ns  | 841.04 ns    | 46.100 ns    | 8400 B    |
| Moq             | 80,217.12 ns  | 28,542.88 ns | 1,564.532 ns | 37600 B   |
| NSubstitute     | 72,503.62 ns  | 36,027.75 ns | 1,974.802 ns | 30848 B   |
| FakeItEasy      | 176,517.53 ns | 59,236.46 ns | 3,246.950 ns | 94400 B   |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for calling methods on mock objects.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-25T02:32:28.119Z*
