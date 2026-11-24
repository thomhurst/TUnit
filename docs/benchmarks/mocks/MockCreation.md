# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-24** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev   | Allocated |
| --------------- | ----------- | --------- | -------- | --------- |
| **TUnit.Mocks** | 39.95 ns    | 0.640 ns  | 0.500 ns | 216 B     |
| Imposter        | 93.31 ns    | 1.572 ns  | 1.393 ns | 360 B     |
| Mockolate       | 23.42 ns    | 0.298 ns  | 0.249 ns | 136 B     |
| Moq             | 1,217.83 ns | 11.083 ns | 9.825 ns | 2040 B    |
| NSubstitute     | 1,890.59 ns | 9.884 ns  | 8.762 ns | 4984 B    |
| FakeItEasy      | 1,663.29 ns | 10.156 ns | 7.930 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 41.73 ns    | 0.818 ns  | 0.725 ns  | 216 B     |
| Imposter        | 146.66 ns   | 2.203 ns  | 1.953 ns  | 568 B     |
| Mockolate       | 22.51 ns    | 0.444 ns  | 0.475 ns  | 136 B     |
| Moq             | 1,315.77 ns | 21.310 ns | 19.933 ns | 1904 B    |
| NSubstitute     | 1,919.23 ns | 14.538 ns | 13.599 ns | 4984 B    |
| FakeItEasy      | 1,630.54 ns | 22.786 ns | 19.027 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against other source-generated and runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-24T02:20:52.556Z*
