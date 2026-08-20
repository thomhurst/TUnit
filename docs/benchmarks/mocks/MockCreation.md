# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 28.69 ns    | 0.472 ns  | 0.442 ns  | 200 B     |
| Imposter        | 92.12 ns    | 0.624 ns  | 0.584 ns  | 440 B     |
| Mockolate       | 17.94 ns    | 0.102 ns  | 0.091 ns  | 160 B     |
| Moq             | 1,363.28 ns | 18.841 ns | 17.624 ns | 2048 B    |
| NSubstitute     | 1,849.19 ns | 27.310 ns | 25.545 ns | 5000 B    |
| FakeItEasy      | 1,717.80 ns | 29.872 ns | 39.878 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 28.77 ns    | 0.420 ns  | 0.372 ns  | 200 B     |
| Imposter        | 144.28 ns   | 0.738 ns  | 0.690 ns  | 696 B     |
| Mockolate       | 17.79 ns    | 0.404 ns  | 0.496 ns  | 176 B     |
| Moq             | 1,366.95 ns | 8.329 ns  | 7.791 ns  | 1912 B    |
| NSubstitute     | 1,887.20 ns | 7.171 ns  | 6.357 ns  | 5000 B    |
| FakeItEasy      | 1,657.78 ns | 25.308 ns | 23.673 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-20T02:41:11.657Z*
