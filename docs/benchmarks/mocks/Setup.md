# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 515.4 ns     | 3.37 ns     | 3.15 ns     | 2.34 KB   |
| Imposter        | 780.8 ns     | 5.60 ns     | 4.96 ns     | 6.12 KB   |
| Mockolate       | 307.6 ns     | 3.78 ns     | 3.54 ns     | 1.41 KB   |
| Moq             | 433,494.1 ns | 2,543.32 ns | 2,379.02 ns | 28.63 KB  |
| NSubstitute     | 6,110.4 ns   | 34.53 ns    | 32.29 ns    | 9.06 KB   |
| FakeItEasy      | 8,404.9 ns   | 53.92 ns    | 50.44 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 741.6 ns     | 2.46 ns   | 2.05 ns   | 3.15 KB   |
| Imposter        | 1,337.1 ns   | 25.65 ns  | 29.54 ns  | 10.59 KB  |
| Mockolate       | 540.1 ns     | 2.00 ns   | 1.87 ns   | 2.35 KB   |
| Moq             | 115,012.3 ns | 631.45 ns | 590.66 ns | 16.35 KB  |
| NSubstitute     | 12,971.3 ns  | 47.79 ns  | 37.31 ns  | 20.31 KB  |
| FakeItEasy      | 7,949.1 ns   | 105.79 ns | 98.95 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-26T02:32:00.095Z*
