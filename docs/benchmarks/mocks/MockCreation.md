# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.14 ns    | 0.119 ns  | 0.106 ns  | 200 B     |
| Imposter        | 74.33 ns    | 0.595 ns  | 0.556 ns  | 440 B     |
| Mockolate       | 13.34 ns    | 0.155 ns  | 0.138 ns  | 160 B     |
| Moq             | 1,001.91 ns | 17.281 ns | 16.165 ns | 2048 B    |
| NSubstitute     | 1,409.14 ns | 26.504 ns | 28.359 ns | 5000 B    |
| FakeItEasy      | 1,315.68 ns | 25.989 ns | 58.128 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 21.88 ns    | 0.089 ns  | 0.083 ns  | 200 B     |
| Imposter        | 115.55 ns   | 0.211 ns  | 0.176 ns  | 696 B     |
| Mockolate       | 13.11 ns    | 0.105 ns  | 0.087 ns  | 176 B     |
| Moq             | 954.74 ns   | 8.128 ns  | 7.205 ns  | 1912 B    |
| NSubstitute     | 1,327.79 ns | 23.492 ns | 21.974 ns | 5000 B    |
| FakeItEasy      | 1,287.94 ns | 22.427 ns | 20.978 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-29T02:35:19.410Z*
