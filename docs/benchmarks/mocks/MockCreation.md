# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 28.31 ns    | 0.192 ns  | 0.180 ns  | 200 B     |
| Imposter        | 90.29 ns    | 0.631 ns  | 0.590 ns  | 440 B     |
| Mockolate       | 16.79 ns    | 0.177 ns  | 0.165 ns  | 160 B     |
| Moq             | 1,295.11 ns | 21.767 ns | 20.361 ns | 2048 B    |
| NSubstitute     | 1,752.71 ns | 5.351 ns  | 4.743 ns  | 5000 B    |
| FakeItEasy      | 1,617.63 ns | 8.652 ns  | 7.225 ns  | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 28.36 ns    | 0.310 ns  | 0.274 ns  | 200 B     |
| Imposter        | 167.33 ns   | 3.366 ns  | 3.306 ns  | 696 B     |
| Mockolate       | 19.59 ns    | 0.458 ns  | 1.350 ns  | 176 B     |
| Moq             | 1,370.53 ns | 15.378 ns | 14.384 ns | 1912 B    |
| NSubstitute     | 2,075.34 ns | 31.017 ns | 29.014 ns | 5000 B    |
| FakeItEasy      | 2,006.40 ns | 8.710 ns  | 7.274 ns  | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-17T02:43:20.076Z*
