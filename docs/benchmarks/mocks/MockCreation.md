# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.97 ns    | 0.518 ns  | 0.484 ns  | 200 B     |
| Imposter        | 99.43 ns    | 1.400 ns  | 1.309 ns  | 440 B     |
| Mockolate       | 19.97 ns    | 0.463 ns  | 0.454 ns  | 160 B     |
| Moq             | 1,424.09 ns | 20.932 ns | 19.580 ns | 2048 B    |
| NSubstitute     | 1,964.82 ns | 19.442 ns | 18.186 ns | 5000 B    |
| FakeItEasy      | 1,845.82 ns | 29.419 ns | 27.519 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.76 ns    | 0.569 ns  | 0.532 ns  | 200 B     |
| Imposter        | 158.42 ns   | 0.696 ns  | 0.617 ns  | 696 B     |
| Mockolate       | 20.63 ns    | 0.477 ns  | 0.446 ns  | 176 B     |
| Moq             | 1,393.56 ns | 6.644 ns  | 6.214 ns  | 1912 B    |
| NSubstitute     | 1,997.15 ns | 6.568 ns  | 5.822 ns  | 5000 B    |
| FakeItEasy      | 1,870.83 ns | 31.917 ns | 36.755 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
