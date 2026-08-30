# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-08-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 517.7 ns     | 1.58 ns     | 1.32 ns     | 2.34 KB   |
| Imposter        | 804.1 ns     | 3.62 ns     | 3.21 ns     | 6.12 KB   |
| Mockolate       | 301.3 ns     | 1.63 ns     | 1.45 ns     | 1.41 KB   |
| Moq             | 418,696.1 ns | 2,699.34 ns | 2,392.90 ns | 28.52 KB  |
| NSubstitute     | 5,994.3 ns   | 24.86 ns    | 20.76 ns    | 9.01 KB   |
| FakeItEasy      | 8,009.3 ns   | 58.37 ns    | 45.57 ns    | 10.45 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 762.8 ns     | 1.83 ns   | 1.53 ns   | 3.15 KB   |
| Imposter        | 1,364.7 ns   | 3.46 ns   | 3.24 ns   | 10.59 KB  |
| Mockolate       | 527.3 ns     | 3.45 ns   | 3.06 ns   | 2.35 KB   |
| Moq             | 113,034.0 ns | 402.10 ns | 356.46 ns | 16.64 KB  |
| NSubstitute     | 12,275.3 ns  | 147.32 ns | 130.60 ns | 20.31 KB  |
| FakeItEasy      | 7,454.1 ns   | 32.39 ns  | 28.71 ns  | 11.71 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-08-30T02:44:44.759Z*
