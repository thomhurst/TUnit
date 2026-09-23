# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 33.88 ns    | 0.724 ns  | 0.775 ns  | 200 B     |
| Imposter        | 103.05 ns   | 0.780 ns  | 0.691 ns  | 440 B     |
| Mockolate       | 19.88 ns    | 0.480 ns  | 0.449 ns  | 160 B     |
| Moq             | 1,254.91 ns | 18.653 ns | 17.448 ns | 2048 B    |
| NSubstitute     | 1,742.01 ns | 4.652 ns  | 3.885 ns  | 5000 B    |
| FakeItEasy      | 1,773.81 ns | 19.091 ns | 17.857 ns | 2723 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 33.35 ns    | 0.733 ns  | 1.003 ns  | 200 B     |
| Imposter        | 165.83 ns   | 3.184 ns  | 3.127 ns  | 696 B     |
| Mockolate       | 22.17 ns    | 0.382 ns  | 0.357 ns  | 176 B     |
| Moq             | 1,314.12 ns | 3.606 ns  | 3.373 ns  | 1912 B    |
| NSubstitute     | 1,864.02 ns | 16.024 ns | 14.989 ns | 5000 B    |
| FakeItEasy      | 1,710.70 ns | 12.801 ns | 11.348 ns | 2723 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-23T02:34:56.618Z*
