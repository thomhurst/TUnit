# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.41 ns    | 0.539 ns  | 0.504 ns  | 200 B     |
| Imposter        | 87.09 ns    | 0.555 ns  | 0.492 ns  | 432 B     |
| Mockolate       | 17.07 ns    | 0.215 ns  | 0.190 ns  | 160 B     |
| Moq             | 1,343.65 ns | 24.373 ns | 22.798 ns | 2048 B    |
| NSubstitute     | 1,883.71 ns | 22.180 ns | 20.747 ns | 5000 B    |
| FakeItEasy      | 1,677.60 ns | 23.484 ns | 21.967 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.21 ns    | 0.358 ns  | 0.317 ns  | 200 B     |
| Imposter        | 135.39 ns   | 2.542 ns  | 2.253 ns  | 688 B     |
| Mockolate       | 16.88 ns    | 0.224 ns  | 0.187 ns  | 176 B     |
| Moq             | 1,366.25 ns | 15.308 ns | 13.570 ns | 1912 B    |
| NSubstitute     | 1,833.08 ns | 18.660 ns | 17.455 ns | 5000 B    |
| FakeItEasy      | 1,689.52 ns | 22.588 ns | 21.129 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-08T02:41:51.713Z*
