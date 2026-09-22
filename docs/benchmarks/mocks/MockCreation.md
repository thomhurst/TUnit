# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.39 ns    | 0.127 ns  | 0.119 ns  | 200 B     |
| Imposter        | 74.38 ns    | 0.134 ns  | 0.125 ns  | 440 B     |
| Mockolate       | 13.05 ns    | 0.084 ns  | 0.075 ns  | 160 B     |
| Moq             | 963.74 ns   | 16.220 ns | 12.664 ns | 2048 B    |
| NSubstitute     | 1,342.48 ns | 8.668 ns  | 7.684 ns  | 5000 B    |
| FakeItEasy      | 1,282.32 ns | 10.148 ns | 8.995 ns  | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.11 ns    | 0.061 ns  | 0.054 ns  | 200 B     |
| Imposter        | 116.24 ns   | 0.858 ns  | 0.716 ns  | 696 B     |
| Mockolate       | 15.61 ns    | 0.191 ns  | 0.169 ns  | 176 B     |
| Moq             | 941.66 ns   | 5.128 ns  | 4.003 ns  | 1912 B    |
| NSubstitute     | 1,320.29 ns | 9.061 ns  | 8.475 ns  | 5000 B    |
| FakeItEasy      | 1,296.39 ns | 14.097 ns | 13.186 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-22T02:33:33.738Z*
