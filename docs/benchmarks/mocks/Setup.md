# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

Last Updated

This benchmark was automatically generated on **2026-11-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 11.0.100

## 📊 Results[​](#-results "Direct link to 📊 Results")

Mock behavior configuration (returns, matchers):

| Library         | Mean         | Error       | StdDev      | Allocated |
| --------------- | ------------ | ----------- | ----------- | --------- |
| **TUnit.Mocks** | 498.3 ns     | 5.91 ns     | 5.53 ns     | 2.38 KB   |
| Imposter        | 291.3 ns     | 4.33 ns     | 3.62 ns     | 1.33 KB   |
| Mockolate       | 291.8 ns     | 5.71 ns     | 5.34 ns     | 1.39 KB   |
| Moq             | 457,727.2 ns | 3,274.76 ns | 3,063.21 ns | 28.52 KB  |
| NSubstitute     | 6,314.6 ns   | 67.26 ns    | 62.92 ns    | 8.97 KB   |
| FakeItEasy      | 7,643.1 ns   | 41.10 ns    | 34.32 ns    | 10.14 KB  |

<!-- -->

***

### Multiple[​](#multiple "Direct link to Multiple")

| Library         | Mean         | Error     | StdDev    | Allocated |
| --------------- | ------------ | --------- | --------- | --------- |
| **TUnit.Mocks** | 727.4 ns     | 10.15 ns  | 9.50 ns   | 3.2 KB    |
| Imposter        | 554.8 ns     | 10.85 ns  | 12.06 ns  | 2.48 KB   |
| Mockolate       | 502.2 ns     | 7.66 ns   | 6.79 ns   | 2.31 KB   |
| Moq             | 112,573.3 ns | 872.90 ns | 816.51 ns | 15.89 KB  |
| NSubstitute     | 12,155.5 ns  | 101.39 ns | 94.84 ns  | 20.17 KB  |
| FakeItEasy      | 7,402.4 ns   | 38.36 ns  | 34.00 ns  | 11.36 KB  |

<!-- -->

## 🎯 Key Insights[​](#-key-insights "Direct link to 🎯 Key Insights")

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

***

Methodology

View the [mock benchmarks overview](/docs/benchmarks/mocks/.md) for methodology details and environment information.

*Last generated: 2026-11-16T02:25:25.447Z*
