# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-08** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 22.89 ns    | 0.116 ns  | 0.109 ns  | 200 B     |
| Imposter        | 81.17 ns    | 1.673 ns  | 2.290 ns  | 440 B     |
| Mockolate       | 13.50 ns    | 0.153 ns  | 0.136 ns  | 160 B     |
| Moq             | 952.21 ns   | 17.987 ns | 16.825 ns | 2048 B    |
| NSubstitute     | 1,355.45 ns | 21.109 ns | 19.745 ns | 5000 B    |
| FakeItEasy      | 1,309.89 ns | 24.840 ns | 28.606 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error    | StdDev   | Allocated |
| --------------- | ----------- | -------- | -------- | --------- |
| **TUnit.Mocks** | 22.85 ns    | 0.172 ns | 0.152 ns | 200 B     |
| Imposter        | 121.68 ns   | 0.664 ns | 0.589 ns | 696 B     |
| Mockolate       | 14.27 ns    | 0.050 ns | 0.042 ns | 176 B     |
| Moq             | 941.66 ns   | 3.348 ns | 2.968 ns | 1912 B    |
| NSubstitute     | 1,329.49 ns | 5.327 ns | 4.722 ns | 5000 B    |
| FakeItEasy      | 1,247.89 ns | 4.051 ns | 3.591 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-08T02:32:39.573Z*
