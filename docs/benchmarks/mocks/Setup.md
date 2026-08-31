# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 539.7 ns     | 7.01 ns     | 6.56 ns     | 2.34 KB   |
| Imposter        | 869.6 ns     | 12.66 ns    | 10.57 ns    | 6.12 KB   |
| Mockolate       | 318.9 ns     | 4.87 ns     | 4.56 ns     | 1.41 KB   |
| Moq             | 433,864.2 ns | 2,999.88 ns | 2,659.31 ns | 28.52 KB  |
| NSubstitute     | 6,307.8 ns   | 35.83 ns    | 29.92 ns    | 9.06 KB   |
| FakeItEasy      | 7,885.3 ns   | 45.54 ns    | 40.37 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 777.1 ns     | 4.61 ns   | 3.85 ns   | 3.15 KB   |
| Imposter        | 1,429.2 ns   | 11.89 ns  | 9.93 ns   | 10.59 KB  |
| Mockolate       | 558.2 ns     | 5.20 ns   | 4.61 ns   | 2.35 KB   |
| Moq             | 118,060.3 ns | 729.79 ns | 609.41 ns | 16.61 KB  |
| NSubstitute     | 12,539.7 ns  | 95.80 ns  | 84.92 ns  | 20.31 KB  |
| FakeItEasy      | 7,957.0 ns   | 84.32 ns  | 70.41 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-31T02:34:36.043Z*
