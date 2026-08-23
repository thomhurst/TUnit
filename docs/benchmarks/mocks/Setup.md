# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 582.2 ns     | 2.39 ns     | 2.24 ns     | 2.34 KB   |
| Imposter        | 870.7 ns     | 11.05 ns    | 10.33 ns    | 6.12 KB   |
| Mockolate       | 342.5 ns     | 2.13 ns     | 1.99 ns     | 1.41 KB   |
| Moq             | 306,528.9 ns | 1,740.92 ns | 1,628.46 ns | 28.52 KB  |
| NSubstitute     | 6,228.6 ns   | 23.77 ns    | 21.07 ns    | 9.01 KB   |
| FakeItEasy      | 7,407.9 ns   | 42.04 ns    | 39.32 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean        | Error     | StdDev    | Allocated |
| --------------- | ----------- | --------- | --------- | --------- |
| **TUnit.Mocks** | 838.6 ns    | 3.96 ns   | 3.71 ns   | 3.15 KB   |
| Imposter        | 1,474.3 ns  | 6.11 ns   | 5.72 ns   | 10.59 KB  |
| Mockolate       | 592.4 ns    | 2.09 ns   | 1.86 ns   | 2.35 KB   |
| Moq             | 89,702.4 ns | 522.77 ns | 436.53 ns | 16.53 KB  |
| NSubstitute     | 11,747.4 ns | 55.47 ns  | 49.17 ns  | 20.31 KB  |
| FakeItEasy      | 7,167.6 ns  | 62.36 ns  | 58.33 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-23T02:45:27.613Z*
