# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.46 ns    | 0.217 ns  | 0.192 ns  | 200 B     |
| Imposter        | 118.83 ns   | 0.719 ns  | 0.601 ns  | 440 B     |
| Mockolate       | 17.01 ns    | 0.244 ns  | 0.229 ns  | 160 B     |
| Moq             | 1,340.32 ns | 21.753 ns | 20.348 ns | 2048 B    |
| NSubstitute     | 1,931.06 ns | 11.105 ns | 10.387 ns | 5000 B    |
| FakeItEasy      | 1,665.47 ns | 10.871 ns | 10.169 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error    | StdDev   | Allocated |
| --------------- | ----------- | -------- | -------- | --------- |
| **TUnit.Mocks** | 27.14 ns    | 0.169 ns | 0.150 ns | 200 B     |
| Imposter        | 141.52 ns   | 0.546 ns | 0.484 ns | 696 B     |
| Mockolate       | 16.96 ns    | 0.123 ns | 0.109 ns | 176 B     |
| Moq             | 1,451.78 ns | 7.249 ns | 6.781 ns | 1912 B    |
| NSubstitute     | 1,863.71 ns | 7.864 ns | 6.971 ns | 5000 B    |
| FakeItEasy      | 1,661.92 ns | 9.450 ns | 8.839 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-01T02:49:56.169Z*
