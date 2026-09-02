# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 29.71 ns    | 0.084 ns  | 0.074 ns  | 200 B     |
| Imposter        | 100.44 ns   | 0.364 ns  | 0.304 ns  | 440 B     |
| Mockolate       | 18.98 ns    | 0.150 ns  | 0.125 ns  | 160 B     |
| Moq             | 1,248.86 ns | 21.344 ns | 19.965 ns | 2048 B    |
| NSubstitute     | 1,767.87 ns | 30.429 ns | 28.463 ns | 5000 B    |
| FakeItEasy      | 1,676.49 ns | 21.547 ns | 17.993 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.25 ns    | 0.145 ns  | 0.121 ns  | 200 B     |
| Imposter        | 158.07 ns   | 0.778 ns  | 0.689 ns  | 696 B     |
| Mockolate       | 17.98 ns    | 0.047 ns  | 0.039 ns  | 176 B     |
| Moq             | 1,268.01 ns | 7.375 ns  | 6.899 ns  | 1912 B    |
| NSubstitute     | 1,776.28 ns | 18.300 ns | 16.222 ns | 5000 B    |
| FakeItEasy      | 1,708.13 ns | 28.914 ns | 25.631 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-02T02:49:53.672Z*
