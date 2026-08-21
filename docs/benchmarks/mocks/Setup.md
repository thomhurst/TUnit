# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev    | Allocated |
| --------------- | ------------ | ----------- | --------- | --------- |
| **TUnit.Mocks** | 576.3 ns     | 11.28 ns    | 11.59 ns  | 2.34 KB   |
| Imposter        | 739.9 ns     | 14.00 ns    | 23.00 ns  | 6.12 KB   |
| Mockolate       | 348.4 ns     | 4.42 ns     | 4.14 ns   | 1.41 KB   |
| Moq             | 179,489.1 ns | 1,024.08 ns | 957.92 ns | 28.46 KB  |
| NSubstitute     | 5,499.8 ns   | 47.38 ns    | 44.32 ns  | 9.01 KB   |
| FakeItEasy      | 5,427.9 ns   | 40.88 ns    | 38.24 ns  | 10.44 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error    | StdDev   | Allocated |
| --------------- | ----------- | -------- | -------- | --------- |
| **TUnit.Mocks** | 851.2 ns    | 10.81 ns | 10.11 ns | 3.15 KB   |
| Imposter        | 1,339.3 ns  | 26.52 ns | 65.06 ns | 10.59 KB  |
| Mockolate       | 590.0 ns    | 11.61 ns | 13.37 ns | 2.35 KB   |
| Moq             | 49,172.5 ns | 90.15 ns | 75.28 ns | 16.63 KB  |
| NSubstitute     | 9,824.5 ns  | 39.78 ns | 33.22 ns | 20.31 KB  |
| FakeItEasy      | 5,495.6 ns  | 24.89 ns | 23.28 ns | 11.78 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-21T02:46:27.792Z*
