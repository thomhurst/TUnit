# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 509.9 ns     | 3.69 ns     | 3.27 ns     | 2.34 KB   |
| Imposter        | 815.1 ns     | 5.95 ns     | 5.28 ns     | 6.12 KB   |
| Mockolate       | 309.1 ns     | 2.32 ns     | 2.06 ns     | 1.41 KB   |
| Moq             | 430,684.0 ns | 1,820.70 ns | 1,614.01 ns | 28.52 KB  |
| NSubstitute     | 6,253.1 ns   | 84.31 ns    | 74.74 ns    | 9.01 KB   |
| FakeItEasy      | 7,739.4 ns   | 40.83 ns    | 36.20 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 760.8 ns     | 9.40 ns   | 8.79 ns   | 3.15 KB   |
| Imposter        | 1,431.4 ns   | 6.84 ns   | 6.07 ns   | 10.59 KB  |
| Mockolate       | 539.6 ns     | 4.39 ns   | 4.11 ns   | 2.35 KB   |
| Moq             | 113,653.2 ns | 545.89 ns | 510.63 ns | 16.53 KB  |
| NSubstitute     | 12,227.8 ns  | 93.23 ns  | 77.85 ns  | 20.31 KB  |
| FakeItEasy      | 7,949.7 ns   | 112.99 ns | 100.16 ns | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-18T02:39:29.373Z*
