# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-10-22** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.402

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 557.6 ns     | 11.11 ns    | 21.41 ns    | 2.38 KB   |
| Imposter        | 839.8 ns     | 16.62 ns    | 31.63 ns    | 5.99 KB   |
| Mockolate       | 326.3 ns     | 5.91 ns     | 5.24 ns     | 1.41 KB   |
| Moq             | 428,072.0 ns | 2,207.91 ns | 1,843.70 ns | 28.6 KB   |
| NSubstitute     | 6,479.2 ns   | 71.56 ns    | 66.93 ns    | 9.06 KB   |
| FakeItEasy      | 7,947.1 ns   | 48.69 ns    | 43.16 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 814.9 ns     | 6.78 ns   | 6.34 ns   | 3.2 KB    |
| Imposter        | 1,510.0 ns   | 28.16 ns  | 24.96 ns  | 10.53 KB  |
| Mockolate       | 596.3 ns     | 11.57 ns  | 11.36 ns  | 2.35 KB   |
| Moq             | 115,779.6 ns | 699.64 ns | 654.44 ns | 16.35 KB  |
| NSubstitute     | 13,083.7 ns  | 83.51 ns  | 78.11 ns  | 20.31 KB  |
| FakeItEasy      | 7,838.3 ns   | 140.06 ns | 124.16 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-10-22T02:34:37.393Z*
