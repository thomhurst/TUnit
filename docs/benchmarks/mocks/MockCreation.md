# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-19** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 28.09 ns    | 0.346 ns  | 0.289 ns  | 200 B     |
| Imposter        | 89.39 ns    | 0.399 ns  | 0.373 ns  | 440 B     |
| Mockolate       | 16.69 ns    | 0.143 ns  | 0.134 ns  | 160 B     |
| Moq             | 1,374.14 ns | 17.275 ns | 16.159 ns | 2048 B    |
| NSubstitute     | 1,800.64 ns | 6.283 ns  | 5.569 ns  | 5000 B    |
| FakeItEasy      | 1,669.06 ns | 5.677 ns  | 5.033 ns  | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 27.30 ns    | 0.089 ns  | 0.083 ns  | 200 B     |
| Imposter        | 139.42 ns   | 0.543 ns  | 0.454 ns  | 696 B     |
| Mockolate       | 17.10 ns    | 0.252 ns  | 0.236 ns  | 176 B     |
| Moq             | 1,312.41 ns | 9.611 ns  | 8.026 ns  | 1912 B    |
| NSubstitute     | 1,849.90 ns | 12.334 ns | 10.934 ns | 5000 B    |
| FakeItEasy      | 1,666.73 ns | 6.014 ns  | 5.331 ns  | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-19T02:42:18.029Z*
