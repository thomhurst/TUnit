# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.29 ns    | 0.292 ns  | 0.273 ns  | 200 B     |
| Imposter        | 86.33 ns    | 0.606 ns  | 0.567 ns  | 440 B     |
| Mockolate       | 16.62 ns    | 0.239 ns  | 0.212 ns  | 160 B     |
| Moq             | 1,346.99 ns | 16.351 ns | 15.294 ns | 2048 B    |
| NSubstitute     | 1,878.51 ns | 10.994 ns | 10.284 ns | 5000 B    |
| FakeItEasy      | 1,739.06 ns | 14.547 ns | 13.607 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 26.49 ns    | 0.252 ns  | 0.235 ns  | 200 B     |
| Imposter        | 134.63 ns   | 1.052 ns  | 0.933 ns  | 696 B     |
| Mockolate       | 16.45 ns    | 0.059 ns  | 0.055 ns  | 176 B     |
| Moq             | 1,323.73 ns | 7.731 ns  | 6.853 ns  | 1912 B    |
| NSubstitute     | 1,771.98 ns | 6.556 ns  | 5.812 ns  | 5000 B    |
| FakeItEasy      | 1,714.98 ns | 15.089 ns | 13.376 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-27T02:37:02.890Z*
