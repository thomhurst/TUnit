# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 30.17 ns    | 0.157 ns  | 0.139 ns  | 200 B     |
| Imposter        | 114.32 ns   | 0.677 ns  | 0.633 ns  | 440 B     |
| Mockolate       | 17.87 ns    | 0.102 ns  | 0.091 ns  | 160 B     |
| Moq             | 1,258.98 ns | 23.463 ns | 21.947 ns | 2048 B    |
| NSubstitute     | 1,722.10 ns | 6.319 ns  | 5.602 ns  | 5000 B    |
| FakeItEasy      | 1,631.47 ns | 4.825 ns  | 4.029 ns  | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error    | StdDev   | Allocated |
| --------------- | ----------- | -------- | -------- | --------- |
| **TUnit.Mocks** | 29.47 ns    | 0.098 ns | 0.087 ns | 200 B     |
| Imposter        | 157.65 ns   | 0.346 ns | 0.289 ns | 696 B     |
| Mockolate       | 18.12 ns    | 0.122 ns | 0.114 ns | 176 B     |
| Moq             | 1,229.90 ns | 8.635 ns | 7.655 ns | 1912 B    |
| NSubstitute     | 1,735.69 ns | 7.560 ns | 7.071 ns | 5000 B    |
| FakeItEasy      | 1,607.00 ns | 5.785 ns | 5.128 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-18T02:39:29.373Z*
