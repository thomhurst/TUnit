# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.16 ns    | 0.107 ns  | 0.095 ns  | 200 B     |
| Imposter        | 82.36 ns    | 1.165 ns  | 1.090 ns  | 440 B     |
| Mockolate       | 13.60 ns    | 0.267 ns  | 0.250 ns  | 160 B     |
| Moq             | 970.03 ns   | 18.503 ns | 17.308 ns | 2048 B    |
| NSubstitute     | 1,306.95 ns | 9.736 ns  | 8.130 ns  | 5000 B    |
| FakeItEasy      | 1,268.64 ns | 7.785 ns  | 6.901 ns  | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 24.18 ns    | 0.517 ns  | 0.531 ns  | 200 B     |
| Imposter        | 118.65 ns   | 0.470 ns  | 0.417 ns  | 696 B     |
| Mockolate       | 14.59 ns    | 0.141 ns  | 0.132 ns  | 176 B     |
| Moq             | 1,008.08 ns | 12.792 ns | 11.966 ns | 1912 B    |
| NSubstitute     | 1,477.13 ns | 13.008 ns | 12.168 ns | 5000 B    |
| FakeItEasy      | 1,395.24 ns | 15.356 ns | 13.613 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-14T02:37:23.172Z*
