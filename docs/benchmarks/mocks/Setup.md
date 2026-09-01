# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-09-01** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 556.1 ns     | 11.03 ns    | 10.32 ns    | 2.34 KB   |
| Imposter        | 876.0 ns     | 16.70 ns    | 18.56 ns    | 6.12 KB   |
| Mockolate       | 331.0 ns     | 6.61 ns     | 7.35 ns     | 1.41 KB   |
| Moq             | 424,302.0 ns | 3,596.70 ns | 3,188.38 ns | 28.63 KB  |
| NSubstitute     | 6,397.8 ns   | 119.21 ns   | 105.67 ns   | 9.01 KB   |
| FakeItEasy      | 8,518.0 ns   | 136.62 ns   | 127.79 ns   | 10.53 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 806.7 ns     | 15.25 ns  | 15.66 ns  | 3.15 KB   |
| Imposter        | 1,442.5 ns   | 28.45 ns  | 27.95 ns  | 10.59 KB  |
| Mockolate       | 561.3 ns     | 10.90 ns  | 13.38 ns  | 2.35 KB   |
| Moq             | 113,601.7 ns | 781.77 ns | 693.02 ns | 16.53 KB  |
| NSubstitute     | 13,317.7 ns  | 186.29 ns | 165.14 ns | 20.5 KB   |
| FakeItEasy      | 8,404.0 ns   | 69.46 ns  | 64.97 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-09-01T02:34:33.391Z*
