# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 33.11 ns    | 0.636 ns  | 0.595 ns  | 200 B     |
| Imposter        | 95.18 ns    | 1.633 ns  | 1.527 ns  | 432 B     |
| Mockolate       | 19.52 ns    | 0.453 ns  | 0.484 ns  | 160 B     |
| Moq             | 1,398.89 ns | 21.007 ns | 19.650 ns | 2048 B    |
| NSubstitute     | 2,002.54 ns | 20.881 ns | 19.532 ns | 5000 B    |
| FakeItEasy      | 1,913.69 ns | 36.884 ns | 39.465 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 33.66 ns    | 0.732 ns  | 0.977 ns  | 200 B     |
| Imposter        | 135.42 ns   | 2.753 ns  | 4.285 ns  | 688 B     |
| Mockolate       | 19.41 ns    | 0.436 ns  | 0.448 ns  | 176 B     |
| Moq             | 1,343.12 ns | 4.457 ns  | 4.169 ns  | 1912 B    |
| NSubstitute     | 1,914.29 ns | 11.966 ns | 11.193 ns | 5000 B    |
| FakeItEasy      | 1,827.78 ns | 21.319 ns | 17.802 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-07T02:41:59.908Z*
