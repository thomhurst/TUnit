# MockCreation Benchmark

> Mock instance creation performance — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock instance creation performance:

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 31.69 ns    | 0.618 ns  | 0.578 ns  | 200 B     |
| Imposter        | 99.83 ns    | 0.165 ns  | 0.138 ns  | 440 B     |
| Mockolate       | 17.61 ns    | 0.098 ns  | 0.092 ns  | 160 B     |
| Moq             | 1,249.58 ns | 24.003 ns | 27.642 ns | 2048 B    |
| NSubstitute     | 1,751.57 ns | 34.434 ns | 33.819 ns | 5000 B    |
| FakeItEasy      | 1,653.45 ns | 32.468 ns | 34.740 ns | 2715 B    |

<!-- -->

***

### Repository[​](#repository "Direct link to Repository")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 29.65 ns    | 0.154 ns  | 0.144 ns  | 200 B     |
| Imposter        | 160.07 ns   | 0.790 ns  | 0.739 ns  | 696 B     |
| Mockolate       | 18.97 ns    | 0.148 ns  | 0.131 ns  | 176 B     |
| Moq             | 1,267.32 ns | 7.336 ns  | 6.862 ns  | 1912 B    |
| NSubstitute     | 1,817.61 ns | 29.093 ns | 27.214 ns | 5000 B    |
| FakeItEasy      | 1,771.10 ns | 34.757 ns | 41.376 ns | 2715 B    |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-27T04:05:27.840Z*
