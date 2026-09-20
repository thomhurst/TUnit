# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.62 ns    | 0.681 ns  | 0.785 ns  | 200 B     |
| Imposter        | 97.45 ns    | 1.994 ns  | 1.865 ns  | 440 B     |
| Mockolate       | 19.98 ns    | 0.461 ns  | 0.717 ns  | 160 B     |
| Moq             | 1,357.22 ns | 26.193 ns | 30.164 ns | 2048 B    |
| NSubstitute     | 1,911.20 ns | 36.976 ns | 34.587 ns | 5000 B    |
| FakeItEasy      | 1,888.30 ns | 35.931 ns | 42.774 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 32.45 ns    | 0.694 ns  | 0.742 ns  | 200 B     |
| Imposter        | 155.17 ns   | 3.089 ns  | 2.739 ns  | 696 B     |
| Mockolate       | 19.52 ns    | 0.454 ns  | 1.317 ns  | 176 B     |
| Moq             | 1,467.38 ns | 8.507 ns  | 7.957 ns  | 1912 B    |
| NSubstitute     | 2,004.77 ns | 29.827 ns | 26.441 ns | 5000 B    |
| FakeItEasy      | 1,868.97 ns | 35.590 ns | 34.955 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-20T02:32:50.293Z*
